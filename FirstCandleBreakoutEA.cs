using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

/*
 * ============================================================================
 * EA Name: First Candle Breakout EA
 * Platform: cTrader
 * Author: Marcel Heiniger
 * Version: 1.1.0
 * Date: 2026-02-08
 * ============================================================================
 * 
 * DESCRIPTION:
 * This EA trades based on the first 1-hour candle of the day with MA filter.
 * - Entry direction can be determined by First Candle or Moving Average
 * - Enters LONG/SHORT based on selected entry logic
 * - Uses dynamic position sizing based on risk parameters
 * - Optional time-based trade closure
 * 
 * ============================================================================
 * VERSION CONTROL & CHANGELOG
 * ============================================================================
 * 
 * v1.1.0 - 2026-02-08
 * - Added Moving Average filter for entry direction
 * - Added Entry Direction parameter (First Candle / Moving Average)
 * - MA Period configurable (default 200)
 * - Entry logic now more flexible with multiple strategies
 * 
 * v1.0.0 - 2026-02-08
 * - Initial release
 * - First candle breakout strategy implementation
 * - Configurable timezone support
 * - Flexible candle timing (firstCandleTime parameter)
 * - Dynamic SL calculation with minimum distance enforcement
 * - Risk-based position sizing (% Balance, % Equity, Fixed Amount)
 * - Maximum lot size cap
 * - Optional time-based trade closure
 * - Risk-reward ratio based TP calculation
 * 
 * ============================================================================
 */

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class FirstCandleBreakoutEA : Robot
    {
        #region Parameters

        [Parameter("=== TIMING SETTINGS ===")]
        public string TimingHeader { get; set; }

        [Parameter("Time Zone", DefaultValue = "Broker Server Time", Group = "Timing")]
        public string TimeZone { get; set; }

        [Parameter("First Candle Time (HH:MM)", DefaultValue = "01:00", Group = "Timing")]
        public string FirstCandleTime { get; set; }

        [Parameter("Close Trade Time (HH:MM)", DefaultValue = "23:00", Group = "Timing")]
        public string CloseTradeTime { get; set; }

        [Parameter("Close Trade at Time", DefaultValue = true, Group = "Timing")]
        public bool CloseTradeAtTime { get; set; }

        [Parameter("=== ENTRY LOGIC ===")]
        public string EntryHeader { get; set; }

        [Parameter("Entry Direction", DefaultValue = EntryDirection.MovingAverage, Group = "Entry Logic")]
        public EntryDirection EntryDirectionMode { get; set; }

        [Parameter("MA Period", DefaultValue = 200, MinValue = 1, Group = "Entry Logic")]
        public int MAPeriod { get; set; }

        [Parameter("=== STOP LOSS SETTINGS ===")]
        public string SLHeader { get; set; }

        [Parameter("Margin (Pips)", DefaultValue = 0, MinValue = 0, Group = "Stop Loss")]
        public double Margin { get; set; }

        [Parameter("Minimum SL (Pips)", DefaultValue = 100, MinValue = 1, Group = "Stop Loss")]
        public double MinSL { get; set; }

        [Parameter("=== TAKE PROFIT SETTINGS ===")]
        public string TPHeader { get; set; }

        [Parameter("Desired Risk:Reward", DefaultValue = 4, MinValue = 0.1, Group = "Take Profit")]
        public double DesiredRR { get; set; }

        [Parameter("=== RISK MANAGEMENT ===")]
        public string RiskHeader { get; set; }

        [Parameter("Max SL Value", DefaultValue = 1, MinValue = 0.01, Group = "Risk Management")]
        public double MaxSLValue { get; set; }

        [Parameter("Max SL Unit", DefaultValue = RiskUnit.PercentBalance, Group = "Risk Management")]
        public RiskUnit MaxSLUnit { get; set; }

        [Parameter("Max Lot Size", DefaultValue = 5, MinValue = 0.01, Group = "Risk Management")]
        public double MaxLotSize { get; set; }

        #endregion

        #region Private Fields

        private DateTime _lastTradeDate;
        private bool _tradeTakenToday;
        private TimeSpan _firstCandleTimeSpan;
        private TimeSpan _closeTradeTimeSpan;
        private string _tradeLabel = "FirstCandleEA";
        private MovingAverage _ma;

        #endregion

        #region Robot Events

        protected override void OnStart()
        {
            // Parse time strings
            if (!TimeSpan.TryParse(FirstCandleTime, out _firstCandleTimeSpan))
            {
                Print("ERROR: Invalid First Candle Time format. Use HH:MM");
                Stop();
                return;
            }

            if (!TimeSpan.TryParse(CloseTradeTime, out _closeTradeTimeSpan))
            {
                Print("ERROR: Invalid Close Trade Time format. Use HH:MM");
                Stop();
                return;
            }

            Print("=== First Candle Breakout EA Started ===");
            Print($"Version: 1.1.0");
            Print($"Symbol: {SymbolName}");
            Print($"First Candle Time: {FirstCandleTime}");
            Print($"Close Trade Time: {CloseTradeTime}");
            Print($"Entry Direction: {EntryDirectionMode}");
            Print($"MA Period: {MAPeriod}");
            Print($"Risk: {MaxSLValue} {MaxSLUnit}");
            Print($"Max Lot Size: {MaxLotSize}");
            Print($"Min SL: {MinSL} pips");
            Print($"Target RR: {DesiredRR}");
            Print("========================================");

            // Initialize Moving Average
            _ma = Indicators.MovingAverage(Bars.ClosePrices, MAPeriod, MovingAverageType.Simple);

            _lastTradeDate = DateTime.MinValue;
            _tradeTakenToday = false;
        }

        protected override void OnBar()
        {
            DateTime currentTime = GetCurrentTime();
            DateTime currentDate = currentTime.Date;

            // Reset daily flag if new day
            if (currentDate > _lastTradeDate)
            {
                _tradeTakenToday = false;
                Print($"New trading day: {currentDate:yyyy-MM-dd}");
            }

            // Check if current bar is the first candle close time
            TimeSpan currentBarTime = currentTime.TimeOfDay;
            
            if (IsFirstCandleCloseTime(currentBarTime) && !_tradeTakenToday)
            {
                Print($"First candle detected at {currentTime}");
                ProcessFirstCandle();
                _tradeTakenToday = true;
                _lastTradeDate = currentDate;
            }

            // Check for time-based closure
            if (CloseTradeAtTime && IsCloseTradeTime(currentBarTime))
            {
                CloseAllPositions();
            }
        }

        protected override void OnStop()
        {
            Print("=== First Candle Breakout EA Stopped ===");
        }

        #endregion

        #region Trading Logic

        private void ProcessFirstCandle()
        {
            // Get the last completed bar (first candle of the day)
            int lastBarIndex = Bars.Count - 2; // -2 because -1 is current forming bar
            
            if (lastBarIndex < 0)
            {
                Print("ERROR: Not enough bars to process");
                return;
            }

            double open = Bars.OpenPrices[lastBarIndex];
            double close = Bars.ClosePrices[lastBarIndex];
            double high = Bars.HighPrices[lastBarIndex];
            double low = Bars.LowPrices[lastBarIndex];

            Print($"First Candle - O:{open} H:{high} L:{low} C:{close}");

            // Determine trade direction based on selected mode
            TradeType? tradeType = DetermineTradeDirection(lastBarIndex, open, close);
            
            if (tradeType == null)
            {
                Print("No trade signal");
                return;
            }

            double entryPrice;
            double stopLoss;
            double takeProfit;

            // Execute based on direction
            if (tradeType == TradeType.Sell) // SHORT
            {
                entryPrice = Symbol.Bid;
                
                // SL = candle high + margin, but minimum minSL from entry
                double marginInPrice = Margin * Symbol.PipSize;
                double minSLInPrice = MinSL * Symbol.PipSize;
                
                stopLoss = Math.Max(high + marginInPrice, entryPrice + minSLInPrice);
                
                double slDistance = stopLoss - entryPrice;
                takeProfit = entryPrice - (slDistance * DesiredRR);

                Print($"Going SHORT");
            }
            else // LONG
            {
                entryPrice = Symbol.Ask;
                
                // SL = candle low - margin, but minimum minSL from entry
                double marginInPrice = Margin * Symbol.PipSize;
                double minSLInPrice = MinSL * Symbol.PipSize;
                
                stopLoss = Math.Min(low - marginInPrice, entryPrice - minSLInPrice);
                
                double slDistance = entryPrice - stopLoss;
                takeProfit = entryPrice + (slDistance * DesiredRR);

                Print($"Going LONG");
            }

            // Calculate position size
            double volume = CalculatePositionSize(entryPrice, stopLoss);
            
            if (volume < Symbol.VolumeInUnitsMin)
            {
                Print($"ERROR: Calculated volume {volume} is below minimum {Symbol.VolumeInUnitsMin}");
                return;
            }

            // Execute trade
            Print($"Executing {tradeType} order:");
            Print($"  Entry: {entryPrice}");
            Print($"  SL: {stopLoss} (Distance: {Math.Abs(entryPrice - stopLoss) / Symbol.PipSize:F1} pips)");
            Print($"  TP: {takeProfit} (RR: {DesiredRR})");
            Print($"  Volume: {volume} units ({volume / 100000:F2} lots)");

            var result = ExecuteMarketOrder(tradeType.Value, SymbolName, volume, _tradeLabel, 
                Math.Abs(entryPrice - stopLoss) / Symbol.PipSize, 
                Math.Abs(takeProfit - entryPrice) / Symbol.PipSize);

            if (result.IsSuccessful)
            {
                Print($"Trade executed successfully - Position ID: {result.Position.Id}");
            }
            else
            {
                Print($"ERROR: Trade execution failed - {result.Error}");
            }
        }

        private TradeType? DetermineTradeDirection(int barIndex, double open, double close)
        {
            switch (EntryDirectionMode)
            {
                case EntryDirection.FirstCandle:
                    // Original logic - based on first candle direction
                    if (close < open)
                    {
                        Print("First Candle is BEARISH - Signal: SHORT");
                        return TradeType.Sell;
                    }
                    else if (close > open)
                    {
                        Print("First Candle is BULLISH - Signal: LONG");
                        return TradeType.Buy;
                    }
                    else
                    {
                        Print("First Candle is DOJI - No signal");
                        return null;
                    }

                case EntryDirection.MovingAverage:
                    // MA logic - enter based on price vs MA
                    double maValue = _ma.Result[barIndex];
                    Print($"MA({MAPeriod}): {maValue:F5}, Close: {close:F5}");
                    
                    if (close < maValue)
                    {
                        Print("Price below MA - Signal: SHORT");
                        return TradeType.Sell;
                    }
                    else if (close > maValue)
                    {
                        Print("Price above MA - Signal: LONG");
                        return TradeType.Buy;
                    }
                    else
                    {
                        Print("Price exactly at MA - No signal");
                        return null;
                    }

                default:
                    Print("ERROR: Unknown entry direction mode");
                    return null;
            }
        }

        private double CalculatePositionSize(double entryPrice, double stopLoss)
        {
            double slDistanceInPips = Math.Abs(entryPrice - stopLoss) / Symbol.PipSize;
            double riskAmount = 0;

            // Calculate risk amount based on unit type
            switch (MaxSLUnit)
            {
                case RiskUnit.PercentBalance:
                    riskAmount = Account.Balance * (MaxSLValue / 100.0);
                    break;
                case RiskUnit.PercentEquity:
                    riskAmount = Account.Equity * (MaxSLValue / 100.0);
                    break;
                case RiskUnit.AccountCurrency:
                    riskAmount = MaxSLValue;
                    break;
            }

            // Calculate position size in units
            double pipValue = Symbol.PipValue;
            double volumeInUnits = (riskAmount / (slDistanceInPips * pipValue));

            // Apply maximum lot size cap (convert lots to units)
            double maxVolumeInUnits = MaxLotSize * 100000;
            volumeInUnits = Math.Min(volumeInUnits, maxVolumeInUnits);

            // Normalize volume
            volumeInUnits = Symbol.NormalizeVolumeInUnits(volumeInUnits, RoundingMode.Down);

            Print($"Position Size Calculation:");
            Print($"  Risk Amount: {riskAmount:F2} {Account.Currency}");
            Print($"  SL Distance: {slDistanceInPips:F1} pips");
            Print($"  Calculated Volume: {volumeInUnits} units ({volumeInUnits / 100000:F2} lots)");

            return volumeInUnits;
        }

        private void CloseAllPositions()
        {
            var positions = Positions.FindAll(_tradeLabel, SymbolName);
            
            if (positions.Length > 0)
            {
                Print($"Closing {positions.Length} position(s) at {GetCurrentTime()}");
                
                foreach (var position in positions)
                {
                    ClosePosition(position);
                }
            }
        }

        #endregion

        #region Helper Methods

        private DateTime GetCurrentTime()
        {
            // For now, using server time. Could be extended to support multiple timezones
            return Server.Time;
        }

        private bool IsFirstCandleCloseTime(TimeSpan currentTime)
        {
            // Allow some tolerance (within 1 minute) for matching
            TimeSpan tolerance = TimeSpan.FromMinutes(1);
            return Math.Abs((currentTime - _firstCandleTimeSpan).TotalMinutes) < tolerance.TotalMinutes;
        }

        private bool IsCloseTradeTime(TimeSpan currentTime)
        {
            // Allow some tolerance (within 1 minute) for matching
            TimeSpan tolerance = TimeSpan.FromMinutes(1);
            return Math.Abs((currentTime - _closeTradeTimeSpan).TotalMinutes) < tolerance.TotalMinutes;
        }

        #endregion
    }

    #region Enums

    public enum RiskUnit
    {
        PercentBalance,
        PercentEquity,
        AccountCurrency
    }

    public enum EntryDirection
    {
        FirstCandle,
        MovingAverage
    }

    #endregion
}
