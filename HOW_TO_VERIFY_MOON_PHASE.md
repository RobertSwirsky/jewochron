# How to Run Moon Phase Verification

## Quick Verification (No Installation Required)

The easiest way to verify the moon phase calculation is to compare with online sources:

### 1. Current Moon Phase

**Online Reference**: https://www.timeanddate.com/moon/phases/

**In Your App**:
- The Moon & Molad card shows current moon phase
- Should display emoji, name, illumination %, and day of cycle
- Compare with the online source

### 2. Visual Inspection

**What to look for**:
- Moon in skyline should match current real-world phase
- During day (7am-5pm): Moon should be subtle/faint
- During night (8pm-5am): Moon should be bright and clear
- At dawn/dusk: Moon should smoothly fade in/out

### 3. Phase Progression Test

**Method**:
- Note current moon phase when you run the app
- Check again in 3-4 days
- Moon should have visibly progressed through the cycle
- Waxing: Getting fuller (right side fills first)
- Waning: Getting thinner (left side stays lit)

---

## Advanced Verification (Requires dotnet-script)

If you want to run the automated test suite:

### Install dotnet-script

```powershell
dotnet tool install -g dotnet-script
```

### Run the Test

```powershell
cd C:\Users\rober\Documents\repos\jewochron
dotnet script TestMoonPhase.csx
```

### Expected Output

You should see 10 test cases run, all passing with ✓ marks.
See `EXPECTED_TEST_RESULTS.md` for sample output.

---

## Manual Calculation Verification

### Example: Verify Today's Moon Phase

1. **Go to**: https://www.moongiant.com/phase/today/
2. **Note**: Illumination percentage and phase name
3. **Run your app**
4. **Compare**: Should be within ±2%

### Example Dates to Check

| Date | Expected Phase | Expected Illumination |
|------|---------------|----------------------|
| Jan 29, 2025 | New Moon | ~0% |
| Feb 5, 2025 | First Quarter | ~50% |
| Feb 12, 2025 | Full Moon | ~100% |
| Feb 20, 2025 | Last Quarter | ~50% |

---

## Testing the Time-Based Opacity

### Morning (7am-10am Jerusalem time)
- Moon should be **faint** (30% opacity)
- Barely visible against bright sky
- Shadow and glow subtle

### Afternoon (12pm-5pm Jerusalem time)
- Moon should still be **faint**
- Maintains 30% opacity throughout day

### Evening (5pm-7pm Jerusalem time)
- Moon should **fade in** from 30% → 100%
- Gradual transition as sun sets

### Night (8pm-5am Jerusalem time)
- Moon should be **bright** (100% opacity)
- Clear shadow showing phase
- Visible glow

### Dawn (5am-7am Jerusalem time)
- Moon should **fade out** from 100% → 30%
- Gradual transition as sun rises

---

## Checking Waxing vs Waning

### Waxing Moon (0-14 days after new moon)
- **Right side illuminates first**
- Shadow on **left side**
- Shadow slides **left** as moon gets fuller
- Emoji: 🌑 → 🌒 → 🌓 → 🌔 → 🌕

### Waning Moon (14-29 days after new moon)
- **Left side stays lit**
- Shadow on **right side**
- Shadow slides **right** as moon gets thinner
- Emoji: 🌕 → 🌖 → 🌗 → 🌘 → 🌑

### Visual Test
1. Check current phase online
2. Determine if waxing or waning
3. Verify shadow is on correct side in your app

---

## Known Good Dates for Testing

### 2025 Reference Points

```
Jan 29, 2025 - New Moon 🌑
  - Illumination: 0%
  - Shadow should fully cover lit disc
  - Very faint appearance

Feb 5, 2025 - First Quarter 🌓
  - Illumination: 50%
  - Right half visible (waxing)
  - Shadow covering left half

Feb 12, 2025 - Full Moon 🌕
  - Illumination: 100%
  - Shadow should be nearly invisible
  - Maximum glow

Feb 20, 2025 - Last Quarter 🌗
  - Illumination: 50%
  - Left half visible (waning)
  - Shadow covering right half

Feb 28, 2025 - New Moon 🌑
  - Illumination: 0%
  - Cycle repeats
```

---

## Troubleshooting

### Moon appears too bright during daytime
- Check that Jerusalem time is correct
- Verify time-of-day logic in UpdateSkylineMoonPhase
- Should be using `timeOfDay` parameter (0-24)

### Moon phase doesn't match online sources
- Verify system clock is accurate
- Remember: We use UTC for calculations
- Check internet source is showing current UTC time
- Allow ±12 hour timing difference (normal for simplified algorithm)

### Shadow looks wrong (two circles visible)
- This was fixed in recent update
- Shadow should always overlap the lit disc
- Max offset is 18px (less than 20px radius)
- Shadow color should blend with sky (#2C3E50)

### Phase name is wrong
- Check illumination percentage first
- Phase boundaries in code:
  - New: 0-6.25%
  - Waxing Crescent: 6.25-18.75%
  - First Quarter: 18.75-31.25%
  - Waxing Gibbous: 31.25-43.75%
  - Full: 43.75-56.25%
  - Waning Gibbous: 56.25-68.75%
  - Last Quarter: 68.75-81.25%
  - Waning Crescent: 81.25-93.75%

---

## Success Criteria

✅ **Visual appearance matches real moon**
✅ **Illumination % within ±3% of online source**
✅ **Phase name matches (or is adjacent phase)**
✅ **Waxing/waning direction is correct**
✅ **Moon is faint during day, bright at night**
✅ **Smooth transitions at dawn/dusk**
✅ **No visible "two circles" artifact**

If all criteria pass, your moon phase implementation is working correctly!
