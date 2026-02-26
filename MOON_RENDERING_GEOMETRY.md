# Moon Phase Rendering - Geometric Approach

## The Problem with the Old Method

The previous implementation used two overlapping circles:
- A white "lit" circle
- A dark "shadow" circle that slid across it

This created several visual problems:
1. **Two visible circles** - The shadow circle was visible as a separate object
2. **Unrealistic crescents** - The overlap created straight-edge intersections, not curved crescents
3. **Wrong terminator shape** - A circle sliding over a circle doesn't match the actual lunar terminator curve

## The New Geometric Solution

The new implementation renders the moon phase using **accurate path geometry** that matches how the moon actually appears.

### Key Concepts

1. **The Limb** - The outer edge of the moon (a circle)
2. **The Terminator** - The line between light and dark (an ellipse when viewed)
3. **Phase Geometry** - The dark portion is bounded by the limb and terminator

### How It Works

```
Moon Disc (always visible, full circle)
└─ Craters/Texture (subtle grey spots)
└─ Phase Shadow Shape (path geometry overlay)
   ├─ New Moon: Full circle of shadow
   ├─ Crescent: Shadow bounded by terminator ellipse and limb arc
   ├─ Quarter: Exact semicircle
   ├─ Gibbous: Small shadow bounded by terminator and limb
   └─ Full Moon: No shadow (empty geometry)
```

### XAML Structure

```xaml
<Canvas x:Name="MoonCanvas">
    <!-- Glow (soft halo around moon) -->
    <Ellipse x:Name="SkylineMoonGlow" ... />
    
    <!-- Moon Disc (base, always full circle) -->
    <Ellipse x:Name="SkylineMoonDisc" Fill="#FFFDE7" ... />
    
    <!-- Craters (texture on surface) -->
    <Ellipse ... /> (multiple)
    
    <!-- Phase Shadow (geometric path) -->
    <Path x:Name="SkylineMoonPhaseShape" Fill="#2C3E50" />
</Canvas>
```

### Geometry Calculation

The shadow geometry is calculated based on:

**Phase Factor** (0 = new, 1 = full):
```
phase = illumination / 100.0
```

**Terminator Position**:
```
terminatorOffset = (phase * 2 - 1) * radius

Waxing:  terminatorOffset ranges from -radius to +radius (left to right)
Waning:  terminatorOffset ranges from +radius to -radius (right to left)
```

**Path Construction**:

1. **New Moon (phase ≤ 0.02)**:
   - Full circle of shadow covering entire disc

2. **Waxing Crescent (0.02 < phase < 0.5)**:
   - Start at top of terminator
   - Arc along LEFT limb (edge) from top to bottom
   - Elliptical arc back along terminator (inward curve)
   - Creates crescent shadow on left side

3. **Waxing Gibbous (0.5 < phase < 0.98)**:
   - Same logic but terminator moves right
   - Shadow shrinks to small region on left

4. **Full Moon (phase ≥ 0.98)**:
   - Empty geometry (no shadow rendered)

5. **Waning phases**:
   - Same logic but shadow on RIGHT side
   - Arc along right limb instead

### Mathematical Details

**Ellipse Width** (terminator curvature):
```csharp
ellipseWidth = Math.Abs(radius - Math.Abs(terminatorOffset))
```

This creates the characteristic curved terminator:
- At quarter moon: ellipseWidth = radius (semicircle)
- At crescent: ellipseWidth < radius (tight curve)
- At gibbous: ellipseWidth small (nearly straight)

**Arc Segments**:
- **Limb arcs**: Use moon radius, follow circular edge
- **Terminator arcs**: Use ellipse dimensions, create curved shadow edge

## Visual Results

### New Moon (0-3%)
```
  ⚫
Dark disc, barely visible craters
```

### Waxing Crescent (10%)
```
  🌒
Right edge lit, curved shadow on left
```

### First Quarter (50%)
```
  🌓
Right half lit, perfect semicircle
```

### Waxing Gibbous (75%)
```
  🌔
Mostly lit, small curved shadow on left
```

### Full Moon (95-100%)
```
  🌕
Fully lit disc, no shadow
```

### Waning Gibbous (75%)
```
  🌖
Left side stays lit, shadow on right
```

### Last Quarter (50%)
```
  🌗
Left half lit, perfect semicircle
```

### Waning Crescent (10%)
```
  🌘
Left edge lit, curved shadow on right
```

## Advantages Over Old Method

✅ **Accurate crescents** - Proper curved terminator matching real moon
✅ **No artifacts** - Single unified shape, no "two circles"
✅ **Smooth transitions** - Geometry smoothly morphs through phases
✅ **Realistic appearance** - Matches astronomical photographs
✅ **Efficient rendering** - Single path vs multiple overlapping elements

## Time-of-Day Integration

The geometry combines with time-based opacity:

```csharp
// Daytime (7am-5pm): 30% opacity
// Night (8pm-5am): 100% opacity
// Dawn/Dusk: Smooth fade

moonDisc.Opacity = (0.5 + illuminationFactor * 0.5) * timeOpacityFactor;
shadowShape.Opacity = (1.0 - illuminationFactor * 0.95) * timeOpacityFactor;
```

Result:
- Daytime: Subtle, realistic moon in bright sky
- Nighttime: Bright, clear moon with accurate phase

## Code Structure

**`UpdateSkylineMoonPhase(illuminationPercent, timeOfDay)`**:
1. Calculate phase factor (0-1)
2. Determine waxing/waning from moon age
3. Calculate time-based opacity
4. Generate phase geometry
5. Apply to Path element

**`CreateMoonPhaseGeometry(centerX, centerY, radius, phase, isWaxing)`**:
1. Handle special cases (new moon, full moon)
2. Calculate terminator position
3. Construct PathFigure with arc segments
4. Return PathGeometry

## Testing the Rendering

Run the app and observe:

1. **New moon**: Should be barely visible dark disc
2. **Crescent**: Should show thin curved sliver (not straight edge)
3. **Quarter**: Should be perfect half-moon (not bulging or pinched)
4. **Gibbous**: Should show mostly lit with small curved shadow
5. **Full moon**: Should be completely lit circle

Compare with https://www.timeanddate.com/moon/phases/ for visual accuracy.

## Future Enhancements (Optional)

Possible improvements:
- Add subtle gradient to shadow for 3D depth
- Enhance crater details based on phase
- Add earthshine effect during crescent phases
- Animate smooth transitions between phases

The current implementation provides accurate, realistic moon phases suitable for the clock display!
