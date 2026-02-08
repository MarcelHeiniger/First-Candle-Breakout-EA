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
- ✅ **Trailing Stop** - optional intelligent profit protection
- ✅ **Draw Down Protection System** - automatically reduces risk and stops trading during drawdowns
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

### Risk Management

| Parameter | Default | Description |
|-----------|---------|-------------|
| Risk Per Trade | 1 | Amount to risk per trade |
| Risk Unit | % Balance | Risk unit: % Balance, % Equity, or Account Currency |
| Max Lot Size | 5 | Maximum position size cap (lots) |

### Timing Settings

| Parameter | Default | Description |
|-----------|---------|-------------|
| Time Zone | Broker Server Time | Reference timezone for timing |
| First Candle Time | 01:00 | Time when the first candle closes (HH:MM) |
| Close Trade Time | 21:30 | Time to close open trades (HH:MM) |
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

### Trailing Stop

| Parameter | Default | Description |
|-----------|---------|-------------|
| Enable Trailing Stop | No | Enable/disable trailing stop feature |
| Trailing Start (%) | 50 | Profit level (% of TP distance) to activate trailing |
| Trailing Distance (%) | 25 | Trail distance as % of TP distance |

### Draw Down Protection

| Parameter | Default | Description |
|-----------|---------|-------------|
| Draw Down Base | Balance High Watermark | Base for DD calculation: Balance High Watermark, Equity High Watermark, or Starting Balance |
| Start Protect Draw Down (%) | 5 | Drawdown level that triggers risk reduction |
| Reduce Risk By (%) | 50 | Percentage to reduce risk when in protection mode |
| Stay Protected Until (%) | 3 | Must recover to this level before returning to full risk |
| Max Draw Down (%) | 9 | Maximum drawdown - stops all trading when reached |
| Max DD Base | Max Balance | Reference for Max DD: Max Balance (dynamic) or Start Account Value (static) |
| Start Account Value | 10000 | Fixed reference value when using Start Account Value mode |

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

### Trailing Stop (Optional)

The EA includes an intelligent trailing stop feature to lock in profits:

**How It Works:**

1. **Activation Trigger:**
   - Activates when profit reaches a % of TP distance (default: 50%)
   - Example: TP is 400 pips away, trail activates at 200 pips profit

2. **Trail Distance Calculation:**
   - Calculated ONCE when trailing activates
   - Distance = % of TP distance (default: 25%)
   - Example: TP is 400 pips, trail distance = 100 pips (25% of 400)

3. **Updates:**
   - Checked and updated on each candle close
   - SL moves only in favorable direction (never reversed)
   - LONG: SL moves up only
   - SHORT: SL moves down only

**Example Scenario:**
- Entry: 100.00
- TP: 104.00 (400 pips)
- SL: 96.00 (400 pips)
- Trailing Start: 50% → Activates at profit of 200 pips (102.00)
- Trailing Distance: 25% → Trail at 100 pips (25% of 400)
- At 102.00: Trailing activates, SL moves to 101.00 (102.00 - 100 pips)
- At 103.00: SL moves to 102.00
- At 103.50: SL moves to 102.50
- If price reverses: SL stays at highest level (locks in profit)

### Draw Down Protection

The EA includes an intelligent protection system to preserve capital during drawdown periods:

**Draw Down Calculation:**
There are TWO separate draw down calculations:

1. **Protection Draw Down** (for risk reduction):
   - Uses "Draw Down Base" parameter
   - Calculated from high watermark (dynamic)
   - Triggers risk reduction and protected mode

2. **Max Draw Down** (for stopping trading):
   - Uses "Max DD Base" parameter
   - Two modes available:
     - **Max Balance (default)**: DD from highest balance reached (dynamic, updates as you profit)
     - **Start Account Value**: DD from fixed starting value (static, useful for accounts with pre-existing trades)

**Max DD Base Modes Explained:**

*Max Balance Mode (Dynamic):*
- Reference updates as balance grows
- Example: Start at $10k, grow to $12k → Max DD reference becomes $12k
- 9% Max DD = stops trading if balance drops below $10,920 ($12k - 9%)
- **Use this for:** Fresh accounts or when you want protection to scale with profits

*Start Account Value Mode (Static):*
- Reference is fixed at your specified starting value
- Example: Set Start Account Value = $10k, current balance = $8k (you're already down)
- 9% Max DD = stops trading if balance drops below $9,100 ($10k - 9%)
- **Use this for:** Accounts with pre-existing trades, or when resuming EA after a break

**Normal Mode (DD < 3%):**
- Trades with full risk amount (e.g., $100)

**Protected Mode (DD ≥ 5% and < 9%):**
- Risk reduced by configured percentage (default 50%)
- Example: $100 → $50 per trade
- Stays in protected mode until DD recovers to "Stay Protected Until" level (default 3%)

**Trading Suspended (DD ≥ 9%):**
- No new trades opened
- EA waits for manual intervention or account recovery

**Hysteresis Example:**
1. DD reaches 5% → Enter protected mode, risk $50
2. DD rises to 7% → Stay in protected mode, risk $50
3. DD drops to 4% → Still in protected mode (above 3% threshold), risk $50
4. DD drops to 2.5% → Exit protected mode, return to full risk $100
5. DD rises to 4.5% → Not in protected mode yet (below 5%), full risk $100

This prevents the EA from constantly switching between modes during volatile periods.

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

### v1.3.0 - 2026-02-08
- **NEW FEATURE**: Trailing Stop
- Optional trailing stop that activates when profit reaches X% of TP distance
- Trail distance calculated as % of TP distance (calculated once when activated)
- Updates on each candle close
- Configurable activation trigger (default: 50% of TP)
- Configurable trail distance (default: 25% of TP)
- SL only moves in favorable direction (never against position)

### v1.2.5 - 2026-02-08
- Improved parameter organization for better UX
- Reordered Draw Down Protection parameters to follow logical flow
- Parameters now grouped by: measurement → protection → recovery → stop trading

### v1.2.4 - 2026-02-08
- Fixed mid-day start behavior
- EA now correctly waits until next day's First Candle Time when started mid-day
- Prevents partial-day trading (e.g., starting at noon won't trade at 01:00 same day)
- Improved startup logging to show when first trade will be taken

### v1.2.3 - 2026-02-08
- Fixed compiler warnings
- Renamed TimeZone parameter to TimeZoneMode to avoid conflict with inherited member
- Replaced obsolete Account.Currency with Account.Asset.Name
- Code now compiles without warnings

### v1.2.2 - 2026-02-08
- Improved parameter naming: "Max SL Value/Unit" → "Risk Per Trade/Unit" (clearer intent)
- Reordered parameter groups: Risk Management now appears first (most important)
- Better parameter organization for easier configuration
- Improved startup logging with grouped sections

### v1.2.1 - 2026-02-08
- Enhanced Max Draw Down with flexible reference base options
- Added Max DD Base parameter: Max Balance or Start Account Value
- Added Start Account Value parameter (default 10000) for accounts with pre-existing trades
- Max Balance mode: DD calculated from highest balance ever reached (dynamic)
- Start Account Value mode: DD calculated from fixed starting value (static)
- Improved logging for Max DD calculations

### v1.2.0 - 2026-02-08
- **NEW FEATURE**: Draw Down Protection System
- Automatically reduces risk when draw down reaches threshold
- Stops trading when max draw down is reached
- Three draw down base options: Balance High Watermark, Equity High Watermark, Starting Balance
- Configurable protection levels and risk reduction percentage
- Hysteresis: stays in protected mode until recovery threshold reached
- Changed default Close Trade Time to 21:30 (avoids swap fees)

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
