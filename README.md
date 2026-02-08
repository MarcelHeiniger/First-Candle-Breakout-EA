# First Candle Breakout EA

A cTrader Expert Advisor (cBot) that trades based on the first 1-hour candle of the trading day.

## 📊 Strategy Overview

This EA implements a flexible breakout strategy with multiple entry modes:
- **Monitors** the first 1-hour candle of the day (configurable time)
- **Entry Direction Options:**
  - **First Candle Mode**: Enter LONG if bullish, SHORT if bearish
  - **Moving Average Mode**: Enter LONG if price above MA, SHORT if below MA
- **Risk Management** with dynamic position sizing
- **Optional** time-based trade closure

## ✨ Features

- ✅ Flexible entry logic: First Candle or Moving Average based
- ✅ Configurable Moving Average period (default 200)
- ✅ Flexible timing configuration (customize first candle time and close time)
- ✅ Multiple timezone support
- ✅ Dynamic stop loss with minimum distance enforcement
- ✅ Risk-reward ratio based take profit
- ✅ Three risk management modes: % Balance, % Equity, or Fixed Amount
- ✅ Maximum lot size cap for safety
- ✅ One trade per day limit
- ✅ Optional automatic trade closure at specified time
- ✅ Comprehensive logging for debugging

## 🔧 Installation

### Method 1: Using cTrader Automate

1. Open cTrader platform
2. Click on **Automate** tab
3. Click **New cBot**
4. Replace the default code with the content from `FirstCandleBreakoutEA.cs`
5. Click **Build** (Ctrl + B)
6. The EA will appear in your cBots list

### Method 2: Manual File Placement

1. Download `FirstCandleBreakoutEA.cs`
2. Navigate to: `Documents/cAlgo/Sources/Robots/`
3. Copy the file to this directory
4. Restart cTrader or refresh the Automate section

## 📋 Parameters

### Timing Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| Time Zone | Broker Server Time | Reference timezone for timing |
| First Candle Time | 01:00 | Time when the first candle closes (HH:MM) |
| Close Trade Time | 23:00 | Time to close open trades (HH:MM) |
| Close Trade at Time | Yes | Enable/disable time-based closure |

### Entry Logic

| Parameter | Default | Description |
|-----------|---------|-------------|
| Entry Direction | Moving Average | Entry signal mode: First Candle or Moving Average |
| MA Period | 200 | Moving Average period for MA mode |

### Stop Loss Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| Margin | 0 pips | Buffer added to candle high/low |
| Minimum SL | 100 pips | Minimum stop loss distance |

### Take Profit Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| Desired Risk:Reward | 4 | Target risk-reward ratio |

### Risk Management

| Parameter | Default | Description |
|-----------|---------|-------------|
| Max SL Value | 1 | Risk amount (percentage or fixed) |
| Max SL Unit | % Balance | Risk unit: % Balance, % Equity, or Account Currency |
| Max Lot Size | 5 | Maximum position size cap (lots) |

## 📖 How It Works

### Entry Logic

The EA offers two entry modes:

#### Mode 1: First Candle
1. **Wait** for the first 1-hour candle of the day to close (at `First Candle Time`)
2. **Analyze** the candle:
   - If **bearish** (close < open): Enter **SHORT** at market
   - If **bullish** (close > open): Enter **LONG** at market
   - If **doji** (close = open): No trade

#### Mode 2: Moving Average (Default)
1. **Wait** for the first 1-hour candle of the day to close (at `First Candle Time`)
2. **Check** the Moving Average trend during that candle:
   - If **MA trending down** (MA value at close < MA value at open): Enter **SHORT** at market
   - If **MA trending up** (MA value at close > MA value at open): Enter **LONG** at market
   - If **MA flat** (no change): No trade

### Stop Loss Calculation

**For SHORT positions:**
```
SL = MAX(Candle High + Margin, Entry Price + Minimum SL)
```

**For LONG positions:**
```
SL = MIN(Candle Low - Margin, Entry Price - Minimum SL)
```

### Take Profit Calculation

```
TP = Entry Price ± (SL Distance × Desired RR)
```

### Position Sizing

The EA calculates position size based on:
1. Risk amount (from `Max SL Value` and `Max SL Unit`)
2. Stop loss distance in pips
3. Capped at `Max Lot Size`

**Example:**
- Risk: 1% of $10,000 balance = $100
- SL Distance: 100 pips
- Pip Value: $10 per lot
- Position Size: $100 / (100 pips × $10) = 0.1 lots

### Trade Management

- **One trade per day** maximum
- **Optional closure** at specified time (e.g., 23:00)
- Automatic position management with SL and TP

## 📈 Usage Examples

### Trend Following Setup (MA Mode)
```
Entry Direction: Moving Average
MA Period: 200
Max SL Value: 1
Max SL Unit: % Balance
Minimum SL: 100 pips
Desired RR: 4
Max Lot Size: 5
```

### First Candle Breakout Setup
```
Entry Direction: First Candle
Max SL Value: 1.5
Max SL Unit: % Balance
Minimum SL: 80 pips
Desired RR: 3
Max Lot Size: 3
```

### Conservative MA Setup
```
Entry Direction: Moving Average
MA Period: 200
Max SL Value: 0.5
Max SL Unit: % Balance
Minimum SL: 150 pips
Desired RR: 3
Max Lot Size: 2
```

### Aggressive First Candle Setup
```
Entry Direction: First Candle
Max SL Value: 2
Max SL Unit: % Balance
Minimum SL: 50 pips
Desired RR: 5
Max Lot Size: 10
```

### Fixed Dollar Risk
```
Max SL Value: 100
Max SL Unit: Account Currency
Minimum SL: 100 pips
Desired RR: 4
Max Lot Size: 5
```

## ⚠️ Risk Warning

**Trading forex and CFDs involves significant risk and may not be suitable for all investors.**

- Only trade with money you can afford to lose
- Past performance does not guarantee future results
- Always test on a demo account first
- This EA is provided as-is without warranty
- The author is not responsible for any trading losses

## 🧪 Backtesting Recommendations

Before running this EA on a live account:

1. **Backtest** on historical data (minimum 6 months)
2. **Forward test** on demo account (minimum 1 month)
3. **Optimize** parameters for your specific symbol and timeframe
4. **Monitor** performance regularly
5. **Adjust** risk parameters based on results

## 🔄 Version History

### v1.1.3 - 2026-02-08
- Fixed time-based closure to avoid swap fees
- Now uses OnTick() to close positions precisely at specified time
- Positions close exactly at 23:00 instead of waiting for bar close at midnight
- Added flag to prevent multiple close attempts on same day

### v1.1.2 - 2026-02-08
- **CRITICAL FIX**: Position sizing calculation now works correctly for crypto/indices
- Fixed issue where tiny positions (5 units) were created instead of proper size
- Improved position size calculation to use minimum volume as base
- Added more detailed logging for position size calculation

### v1.1.1 - 2026-02-08
- Fixed MA logic to check MA trend direction (slope) instead of price vs MA
- MA now compares MA value at candle close vs candle open
- Removed empty header parameters that created blank input fields
- Improved parameter organization in UI

### v1.1.0 - 2026-02-08
- Added Moving Average filter for entry direction
- Added Entry Direction parameter (First Candle / Moving Average)
- MA Period configurable (default 200)
- Entry logic now more flexible with multiple strategies
- Improved logging for entry signals

### v1.0.0 - 2026-02-08
- Initial release
- First candle breakout strategy implementation
- Configurable timezone support
- Flexible candle timing (firstCandleTime parameter)
- Dynamic SL calculation with minimum distance enforcement
- Risk-based position sizing (% Balance, % Equity, Fixed Amount)
- Maximum lot size cap
- Optional time-based trade closure
- Risk-reward ratio based TP calculation

## 📝 License

MIT License - See LICENSE file for details

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📧 Support

For issues, questions, or suggestions:
- Open an issue on GitHub
- Provide detailed information about your setup
- Include log outputs if reporting bugs

## 🙏 Acknowledgments

- Built for cTrader platform
- Strategy concept based on first candle breakout methodology

---

**Disclaimer:** This software is for educational purposes only. Use at your own risk.
