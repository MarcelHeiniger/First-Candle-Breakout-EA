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
 * Version: 1.3.1
 * Date: 2026-02-08
 * ============================================================================
 * 
 * DESCRIPTION:
 * This EA trades based on the first 1-hour candle of the day with MA filter.
 * - Entry direction can be determined by First Candle or Moving Average trend
 * - Enters LONG/SHORT based on selected entry logic
 * - Uses dynamic position sizing based on risk parameters
 * - Optional time-based trade closure
 * 
 * ============================================================================
 * VERSION CONTROL & CHANGELOG
 * ============================================================================
 * 
 * v1.3.1 - 2026-02-08
 * - Fixed compiler warning: Updated ModifyPosition to use new API with ProtectionType parameter
 * - Code now compiles without warnings
 * 
 * v1.3.0 - 2026-02-08
 * - NEW FEATURE: Trailing Stop
 * - Optional trailing stop that activates when profit reaches X% of TP distance
 * - Trail distance calculated as % of TP distance (calculated once when activated)
 * - Updates on each candle close
 * - Configurable activation trigger (default: 50% of TP)
 * - Configurable trail distance (default: 25% of TP)
 * - SL only moves in favorable direction (never against position)
 * 
 * v1.2.5 - 2026-02-08
 * - Improved parameter organization for better UX
 * - Reordered Draw Down Protection parameters to follow logical flow:
 *   1. DD Base (what to measure)
 *   2. Start Protect (when protection begins)
 *   3. Reduce Risk By (how much to reduce)
 *   4. Stay Protected Until (recovery threshold)
 *   5. Max DD (stop trading threshold)
 *   6. Max DD Base & Start Account Value (max DD settings)
 * 
 * v1.2.4 - 2026-02-08
 * - Fixed mid-day start behavior
 * - EA now correctly waits until next day's First Candle Time when started mid-day
 * - Prevents partial-day trading (e.g., starting at noon won't trade at 01:00 same day)
 * - Improved startup logging to show when first trade will be taken
 * 
 * v1.2.3 - 2026-02-08
 * - Fixed compiler warnings
 * - Renamed TimeZone parameter to TimeZoneMode to avoid conflict with inherited member
 * - Replaced obsolete Account.Currency with Account.Asset.Name
 * - Code now compiles without warnings
 * 
 * v1.2.2 - 2026-02-08
 * - Improved parameter naming: "Max SL Value/Unit" → "Risk Per Trade/Unit" (clearer intent)
 * - Reordered parameter groups: Risk Management now appears first (most important)
 * - Better parameter organization for easier configuration
 * - Improved startup logging with grouped sections
 * 
 * v1.2.1 - 2026-02-08
 * - Enhanced Max Draw Down with flexible reference base options
 * - Added Max DD Base parameter: Max Balance or Start Account Value
 * - Added Start Account Value parameter (default 10000) for accounts with pre-existing trades
 * - Max Balance mode: DD calculated from highest balance ever reached (dynamic)
 * - Start Account Value mode: DD calculated from fixed starting value (static)
 * - Improved logging for Max DD calculations
 * 
 * v1.2.0 - 2026-02-08
 * - NEW FEATURE: Draw Down Protection System
 * - Automatically reduces risk when draw down reaches threshold
 * - Stops trading when max draw down is reached
 * - Three draw down base options: Balance High Watermark, Equity High Watermark, Starting Balance
 * - Configurable protection levels and risk reduction percentage
 * - Hysteresis: stays in protected mode until recovery threshold reached
 * - Changed default Close Trade Time to 21:30 (avoids swap fees)
 * 
 * v1.1.3 - 2026-02-08
 * - Fixed time-based closure to avoid swap fees
 * - Now uses OnTick() to close positions precisely at specified time
 * - Positions close exactly at 23:00 instead of waiting for bar close at midnight
 * - Added flag to prevent multiple close attempts on same day
 * 
 * v1.1.2 - 2026-02-08
 * - CRITICAL FIX: Position sizing calculation now works correctly for crypto/indices
 * - Fixed issue where tiny positions (5 units) were created instead of proper size
 * - Improved position size calculation to use minimum volume as base
 * - Added more detailed logging for position size calculation
 * 
 * v1.1.1 - 2026-02-08
 * - Fixed MA logic to check MA trend direction (slope) instead of price vs MA
 * - MA now compares MA value at candle close vs candle open
 * - Removed empty header parameters that created blank input fields
 * - Improved parameter organization in UI
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

        [Parameter("Risk Per Trade", DefaultValue = 1, MinValue = 0.01, Group = "Risk Management")]
        public double MaxSLValue { get; set; }

        [Parameter("Risk Unit", DefaultValue = RiskUnit.PercentBalance, Group = "Risk Management")]
        public RiskUnit MaxSLUnit { get; set; }

        [Parameter("Max Lot Size", DefaultValue = 5, MinValue = 0.01, Group = "Risk Management")]
        public double MaxLotSize { get; set; }

        [Parameter("Time Zone", DefaultValue = "Broker Server Time", Group = "Timing")]
        public string TimeZoneMode { get; set; }

        [Parameter("First Candle Time (HH:MM)", DefaultValue = "01:00", Group = "Timing")]
        public string FirstCandleTime { get; set; }

        [Parameter("Close Trade Time (HH:MM)", DefaultValue = "21:30", Group = "Timing")]
        public string CloseTradeTime { get; set; }

        [Parameter("Close Trade at Time", DefaultValue = true, Group = "Timing")]
        public bool CloseTradeAtTime { get; set; }

        [Parameter("Entry Direction", DefaultValue = EntryDirection.MovingAverage, Group = "Entry Logic")]
        public EntryDirection EntryDirectionMode { get; set; }

        [Parameter("MA Period", DefaultValue = 200, MinValue = 1, Group = "Entry Logic")]
        public int MAPeriod { get; set; }

        [Parameter("Margin (Pips)", DefaultValue = 0, MinValue = 0, Group = "Stop Loss")]
        public double Margin { get; set; }

        [Parameter("Minimum SL (Pips)", DefaultValue = 100, MinValue = 1, Group = "Stop Loss")]
        public double MinSL { get; set; }

        [Parameter("Desired Risk:Reward", DefaultValue = 4, MinValue = 0.1, Group = "Take Profit")]
        public double DesiredRR { get; set; }

        [Parameter("Enable Trailing Stop", DefaultValue = false, Group = "Trailing Stop")]
        public bool EnableTrailingStop { get; set; }

        [Parameter("Trailing Start (%)", DefaultValue = 50, MinValue = 0, MaxValue = 100, Group = "Trailing Stop")]
        public double TrailingStart { get; set; }

        [Parameter("Trailing Distance (%)", DefaultValue = 25, MinValue = 1, MaxValue = 100, Group = "Trailing Stop")]
        public double TrailingDistance { get; set; }

        [Parameter("Draw Down Base", DefaultValue = DrawDownBase.BalanceHighWatermark, Group = "Draw Down Protection")]
        public DrawDownBase DDBase { get; set; }

        [Parameter("Start Protect Draw Down (%)", DefaultValue = 5, MinValue = 0, MaxValue = 100, Group = "Draw Down Protection")]
        public double StartProtectDD { get; set; }

        [Parameter("Reduce Risk By (%)", DefaultValue = 50, MinValue = 0, MaxValue = 100, Group = "Draw Down Protection")]
        public double ReduceRiskBy { get; set; }

        [Parameter("Stay Protected Until (%)", DefaultValue = 3, MinValue = 0, MaxValue = 100, Group = "Draw Down Protection")]
        public double StayProtectedUntil { get; set; }

        [Parameter("Max Draw Down (%)", DefaultValue = 9, MinValue = 0, MaxValue = 100, Group = "Draw Down Protection")]
        public double MaxDD { get; set; }

        [Parameter("Max DD Base", DefaultValue = MaxDDBase.MaxBalance, Group = "Draw Down Protection")]
        public MaxDDBase MaxDDBaseMode { get; set; }

        [Parameter("Start Account Value", DefaultValue = 10000, MinValue = 1, Group = "Draw Down Protection")]
        public double StartAccountValue { get; set; }

        #endregion

        #region Private Fields

        private DateTime _lastTradeDate;
        private bool _tradeTakenToday;
        private bool _positionsClosedToday;
        private TimeSpan _firstCandleTimeSpan;
        private TimeSpan _closeTradeTimeSpan;
        private string _tradeLabel = "FirstCandleEA";
        private MovingAverage _ma;
        private double _highWatermark;
        private double _maxDDReferenceValue;
        private bool _inProtectedMode;
        private bool _trailingActivated;
        private double _trailingDistanceInPrice;

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
            Print($"Version: 1.3.1");
            Print($"Symbol: {SymbolName}");
            Print($"--- Risk Management ---");
            Print($"Risk Per Trade: {MaxSLValue} {MaxSLUnit}");
            Print($"Max Lot Size: {MaxLotSize}");
            Print($"--- Timing ---");
            Print($"First Candle Time: {FirstCandleTime}");
            Print($"Close Trade Time: {CloseTradeTime}");
            Print($"--- Entry Logic ---");
            Print($"Entry Direction: {EntryDirectionMode}");
            Print($"MA Period: {MAPeriod}");
            Print($"--- Stop Loss & Take Profit ---");
            Print($"Min SL: {MinSL} pips");
            Print($"Margin: {Margin} pips");
            Print($"Target RR: {DesiredRR}");
            Print($"--- Trailing Stop ---");
            Print($"Enabled: {EnableTrailingStop}");
            if (EnableTrailingStop)
            {
                Print($"Trailing Start: {TrailingStart}% of TP distance");
                Print($"Trailing Distance: {TrailingDistance}% of TP distance");
            }
            Print($"--- Draw Down Protection ---");
            Print($"DD Base: {DDBase}");
            Print($"Start Protect: {StartProtectDD}%");
            Print($"Reduce Risk By: {ReduceRiskBy}%");
            Print($"Max DD: {MaxDD}%");
            Print($"Max DD Base: {MaxDDBaseMode}");
            if (MaxDDBaseMode == MaxDDBase.StartAccountValue)
            {
                Print($"Start Account Value: {StartAccountValue:F2}");
            }
            Print($"Stay Protected Until: {StayProtectedUntil}%");
            Print("========================================");

            // Initialize Moving Average
            _ma = Indicators.MovingAverage(Bars.ClosePrices, MAPeriod, MovingAverageType.Simple);

            // Initialize draw down tracking
            _highWatermark = GetDrawDownBaseValue();
            _maxDDReferenceValue = GetMaxDDReferenceValue();
            _inProtectedMode = false;

            Print($"Initial High Watermark: {_highWatermark:F2}");
            Print($"Max DD Reference Value: {_maxDDReferenceValue:F2}");

            // Initialize to current date to prevent trading on partial day
            DateTime currentTime = GetCurrentTime();
            _lastTradeDate = currentTime.Date;
            _tradeTakenToday = true; // Mark today as taken if starting mid-day
            _positionsClosedToday = false;
            
            Print($"EA started at {currentTime:yyyy-MM-dd HH:mm:ss}");
            Print($"First trade will be taken on next trading day after {FirstCandleTime}");
        }

        protected override void OnBar()
        {
            DateTime currentTime = GetCurrentTime();
            DateTime currentDate = currentTime.Date;

            // Reset daily flag if new day
            if (currentDate > _lastTradeDate)
            {
                _tradeTakenToday = false;
                _positionsClosedToday = false;
                _trailingActivated = false;
                Print($"New trading day: {currentDate:yyyy-MM-dd}");
            }

            // Check if current bar is the first candle close time
            TimeSpan currentBarTime = currentTime.TimeOfDay;
            
            if (IsFirstCandleCloseTime(currentBarTime) && !_tradeTakenToday)
            {
                Print($"First candle detected at {currentTime}");
                
                // Update high watermark and check draw down
                UpdateHighWatermark();
                double currentDD = CalculateDrawDown();
                double maxDDAbsolute = CalculateMaxDrawDown();
                
                Print($"Draw Down Status: {currentDD:F2}%");
                Print($"Max DD Check: Current Balance/Equity vs Reference - DD: {maxDDAbsolute:F2}%");
                
                // Check if max draw down reached - stop trading
                if (maxDDAbsolute >= MaxDD)
                {
                    Print($"WARNING: Max Draw Down ({MaxDD}%) reached! Trading suspended.");
                    Print($"Current Max DD: {maxDDAbsolute:F2}% (Base: {MaxDDBaseMode}). No trade will be taken.");
                    _tradeTakenToday = true; // Mark as taken to prevent trade today
                    _lastTradeDate = currentDate;
                    return;
                }
                
                ProcessFirstCandle();
                _tradeTakenToday = true;
                _lastTradeDate = currentDate;
            }

            // Check and update trailing stop on each bar close
            if (EnableTrailingStop)
            {
                UpdateTrailingStop();
            }
        }

        protected override void OnTick()
        {
            // Check for time-based closure on every tick to close precisely at specified time
            if (CloseTradeAtTime && !_positionsClosedToday)
            {
                DateTime currentTime = GetCurrentTime();
                TimeSpan currentTimeOfDay = currentTime.TimeOfDay;
                
                // Close if we've reached or passed the close time
                if (currentTimeOfDay >= _closeTradeTimeSpan)
                {
                    var positions = Positions.FindAll(_tradeLabel, SymbolName);
                    if (positions.Length > 0)
                    {
                        Print($"Close time reached ({currentTime:HH:mm:ss}) - Closing positions");
                        CloseAllPositions();
                        _positionsClosedToday = true;
                    }
                }
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
                    // MA logic - based on MA trend direction during first candle
                    // Compare MA at candle close vs MA at candle open
                    double maAtClose = _ma.Result[barIndex];
                    double maAtOpen = _ma.Result[barIndex - 1]; // Previous bar (candle open)
                    
                    Print($"MA at Open: {maAtOpen:F5}, MA at Close: {maAtClose:F5}");
                    
                    if (maAtClose < maAtOpen)
                    {
                        Print("MA is BEARISH (trending down) - Signal: SHORT");
                        return TradeType.Sell;
                    }
                    else if (maAtClose > maAtOpen)
                    {
                        Print("MA is BULLISH (trending up) - Signal: LONG");
                        return TradeType.Buy;
                    }
                    else
                    {
                        Print("MA is FLAT (no trend) - No signal");
                        return null;
                    }

                default:
                    Print("ERROR: Unknown entry direction mode");
                    return null;
            }
        }

        private double CalculatePositionSize(double entryPrice, double stopLoss)
        {
            double slDistanceInPrice = Math.Abs(entryPrice - stopLoss);
            double baseRiskAmount = 0;

            // Calculate base risk amount based on unit type
            switch (MaxSLUnit)
            {
                case RiskUnit.PercentBalance:
                    baseRiskAmount = Account.Balance * (MaxSLValue / 100.0);
                    break;
                case RiskUnit.PercentEquity:
                    baseRiskAmount = Account.Equity * (MaxSLValue / 100.0);
                    break;
                case RiskUnit.AccountCurrency:
                    baseRiskAmount = MaxSLValue;
                    break;
            }

            // Apply draw down protection risk reduction
            double currentDD = CalculateDrawDown();
            double riskAmount = ApplyDrawDownProtection(baseRiskAmount, currentDD);

            // Calculate position size based on risk and SL distance
            double slDistanceInPips = slDistanceInPrice / Symbol.PipSize;
            
            // Get pip value for minimum volume to calculate per-unit risk
            double minVolume = Symbol.VolumeInUnitsMin;
            double pipValueForMinVolume = Symbol.PipValue * minVolume;
            double riskPerMinVolume = slDistanceInPips * pipValueForMinVolume;
            
            // Calculate how many min volumes we need
            double numberOfMinVolumes = riskAmount / riskPerMinVolume;
            double volumeInUnits = numberOfMinVolumes * minVolume;

            // Apply maximum lot size cap (convert lots to units)
            double maxVolumeInUnits = MaxLotSize * 100000;
            volumeInUnits = Math.Min(volumeInUnits, maxVolumeInUnits);

            // Normalize volume
            volumeInUnits = Symbol.NormalizeVolumeInUnits(volumeInUnits, RoundingMode.Down);

            Print($"Position Size Calculation:");
            Print($"  Base Risk Amount: {baseRiskAmount:F2} {Account.Asset.Name}");
            Print($"  Current Draw Down: {currentDD:F2}%");
            Print($"  Actual Risk Amount: {riskAmount:F2} {Account.Asset.Name}");
            Print($"  SL Distance: {slDistanceInPips:F1} pips ({slDistanceInPrice:F2} price)");
            Print($"  Pip Value (min vol): {pipValueForMinVolume:F5}");
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

        private void UpdateTrailingStop()
        {
            var positions = Positions.FindAll(_tradeLabel, SymbolName);
            
            if (positions.Length == 0)
                return;

            foreach (var position in positions)
            {
                // Calculate profit and TP distance
                double entryPrice = position.EntryPrice;
                double? takeProfit = position.TakeProfit;
                
                if (!takeProfit.HasValue)
                    continue;

                double tpDistance = Math.Abs(takeProfit.Value - entryPrice);
                double currentPrice = position.TradeType == TradeType.Buy ? Symbol.Bid : Symbol.Ask;
                double currentProfit = position.TradeType == TradeType.Buy 
                    ? currentPrice - entryPrice 
                    : entryPrice - currentPrice;

                // Check if trailing should be activated
                double triggerDistance = tpDistance * (TrailingStart / 100.0);
                
                if (!_trailingActivated && currentProfit >= triggerDistance)
                {
                    // Activate trailing - calculate trail distance ONCE
                    _trailingDistanceInPrice = tpDistance * (TrailingDistance / 100.0);
                    _trailingActivated = true;
                    
                    Print($">>> TRAILING STOP ACTIVATED <<<");
                    Print($"Profit reached {TrailingStart}% of TP distance");
                    Print($"Trail Distance: {_trailingDistanceInPrice / Symbol.PipSize:F1} pips ({TrailingDistance}% of TP)");
                }

                // Update trailing stop if activated
                if (_trailingActivated)
                {
                    double newStopLoss;
                    
                    if (position.TradeType == TradeType.Buy)
                    {
                        // LONG: Trail up only
                        newStopLoss = currentPrice - _trailingDistanceInPrice;
                        
                        // Only move SL up, never down
                        if (position.StopLoss.HasValue && newStopLoss <= position.StopLoss.Value)
                            continue;
                    }
                    else
                    {
                        // SHORT: Trail down only
                        newStopLoss = currentPrice + _trailingDistanceInPrice;
                        
                        // Only move SL down, never up
                        if (position.StopLoss.HasValue && newStopLoss >= position.StopLoss.Value)
                            continue;
                    }

                    // Modify the stop loss
                    ModifyPosition(position, newStopLoss, position.TakeProfit, true);
                    Print($"Trailing Stop Updated: SL moved to {newStopLoss:F5} (Trail: {_trailingDistanceInPrice / Symbol.PipSize:F1} pips)");
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

        private double GetDrawDownBaseValue()
        {
            switch (DDBase)
            {
                case DrawDownBase.BalanceHighWatermark:
                    return Account.Balance;
                case DrawDownBase.EquityHighWatermark:
                    return Account.Equity;
                case DrawDownBase.StartingBalance:
                    return Account.Balance; // Starting balance at EA start
                default:
                    return Account.Balance;
            }
        }

        private double GetMaxDDReferenceValue()
        {
            switch (MaxDDBaseMode)
            {
                case MaxDDBase.MaxBalance:
                    // Will be updated dynamically - start with current high watermark
                    return _highWatermark;
                case MaxDDBase.StartAccountValue:
                    return StartAccountValue;
                default:
                    return _highWatermark;
            }
        }

        private void UpdateHighWatermark()
        {
            double currentValue = GetDrawDownBaseValue();
            
            // Only update if current value is higher than watermark
            if (currentValue > _highWatermark)
            {
                Print($"High Watermark Updated: {_highWatermark:F2} → {currentValue:F2}");
                _highWatermark = currentValue;
                
                // Update Max DD reference if using MaxBalance mode
                if (MaxDDBaseMode == MaxDDBase.MaxBalance)
                {
                    _maxDDReferenceValue = _highWatermark;
                    Print($"Max DD Reference Updated: {_maxDDReferenceValue:F2}");
                }
            }
        }

        private double CalculateDrawDown()
        {
            double currentValue = GetDrawDownBaseValue();
            
            if (_highWatermark <= 0)
                return 0;
            
            double drawDown = ((_highWatermark - currentValue) / _highWatermark) * 100.0;
            return Math.Max(0, drawDown); // Never negative
        }

        private double CalculateMaxDrawDown()
        {
            // Calculate draw down from the max DD reference value
            double currentValue = GetDrawDownBaseValue();
            
            if (_maxDDReferenceValue <= 0)
                return 0;
            
            double drawDown = ((_maxDDReferenceValue - currentValue) / _maxDDReferenceValue) * 100.0;
            return Math.Max(0, drawDown); // Never negative
        }

        private double ApplyDrawDownProtection(double baseRiskAmount, double currentDD)
        {
            // Check protection mode transitions
            if (currentDD >= StartProtectDD)
            {
                if (!_inProtectedMode)
                {
                    Print($">>> ENTERING PROTECTED MODE <<<");
                    Print($"Draw Down ({currentDD:F2}%) >= Start Protect DD ({StartProtectDD}%)");
                    _inProtectedMode = true;
                }
                
                // Apply risk reduction
                double reductionMultiplier = 1.0 - (ReduceRiskBy / 100.0);
                double reducedRisk = baseRiskAmount * reductionMultiplier;
                Print($"Risk Reduced: {baseRiskAmount:F2} → {reducedRisk:F2} (Reduction: {ReduceRiskBy}%)");
                return reducedRisk;
            }
            else if (currentDD <= StayProtectedUntil)
            {
                // Recovery threshold reached - exit protected mode
                if (_inProtectedMode)
                {
                    Print($">>> EXITING PROTECTED MODE <<<");
                    Print($"Draw Down ({currentDD:F2}%) <= Stay Protected Until ({StayProtectedUntil}%)");
                    _inProtectedMode = false;
                }
                return baseRiskAmount; // Full risk
            }
            else
            {
                // In the gap between StayProtectedUntil and StartProtectDD
                if (_inProtectedMode)
                {
                    // Stay in protected mode until we reach StayProtectedUntil
                    double reductionMultiplier = 1.0 - (ReduceRiskBy / 100.0);
                    double reducedRisk = baseRiskAmount * reductionMultiplier;
                    Print($"Staying in Protected Mode - Risk: {reducedRisk:F2}");
                    return reducedRisk;
                }
                else
                {
                    // Not in protected mode yet
                    return baseRiskAmount;
                }
            }
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

    public enum DrawDownBase
    {
        BalanceHighWatermark,
        EquityHighWatermark,
        StartingBalance
    }

    public enum MaxDDBase
    {
        MaxBalance,
        StartAccountValue
    }

    #endregion
}
