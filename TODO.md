# Jewochron - TODO List

## 🔧 Configuration & Customization

### High Priority
- [ ] **Customizable Shabbat Times** - Allow users to configure candle lighting and Havdalah time offsets
  - Different communities use different standards:
    - Candle lighting: 18 min (common), 20 min (some), 40 min (Jerusalem), 15 min (some)
    - Havdalah: 42 min (moderate), 50 min (some), 72 min (Rabbeinu Tam)
  - Considerations:
    - Should this be per-location automatic (e.g., Jerusalem = 40 min)?
    - Should users be able to override defaults?
    - Store in app settings or configuration file?
    - Add UI for settings management?

### Medium Priority
- [ ] Add settings page/dialog for user preferences
- [ ] Location override - allow manual location entry if auto-detection fails
- [ ] Time zone selection for non-local displays

## 🎨 UI/UX Enhancements

### Medium Priority
- [ ] Add animations to card transitions in responsive layouts
- [ ] Improve touch/mouse interactions for synagogue kiosk displays
- [ ] Add keyboard shortcuts for common actions

### Low Priority
- [ ] Dark/light mode toggle (currently fixed dark mode)
- [ ] Custom color themes
- [ ] Font size adjustments for accessibility

## 📅 Feature Additions

### High Priority
- [ ] Add Parsha name to Shabbat card (currently shows generic greeting)
- [ ] Weekly Torah reading details (not just Parsha name)
- [ ] Zmanim (detailed prayer time table)

### Medium Priority
- [ ] Monthly calendar view showing all Jewish dates
- [ ] Holiday information cards with customs and explanations
- [ ] Countdown to upcoming holidays with more detail
- [ ] Omer counting (between Pesach and Shavuot)

### Low Priority
- [ ] Daily learning schedules (Daf Yomi already included)
- [ ] Haftarah readings
- [ ] Mishnah Yomi
- [ ] Parsha summary/commentary

## 🐛 Bug Fixes & Improvements

### Needs Investigation
- [ ] Entity Framework Core compatibility with .NET 10 (build warnings)
- [ ] Verify all responsive states work correctly on actual synagogue displays
- [ ] Test location detection in various geographic regions

### Performance
- [ ] Cache Torah portion API responses to reduce network calls
- [ ] Optimize skyline animation performance on lower-end hardware

## 📖 Documentation

### High Priority
- [ ] Create user guide for synagogue installation
- [ ] Document all customization options

### Medium Priority
- [ ] API documentation for services
- [ ] Contributing guidelines
- [ ] Architecture decision records (ADR)

## 🧪 Testing

### Needs Implementation
- [ ] Unit tests for Hebrew calendar calculations
- [ ] Unit tests for time calculations
- [ ] Integration tests for API calls (Torah portion, location)
- [ ] UI tests for responsive layouts

## 🚀 Deployment & Distribution

### Future Considerations
- [ ] Microsoft Store distribution
- [ ] Auto-update mechanism
- [ ] Installation wizard for synagogue IT staff
- [ ] Multi-language support (beyond English/Hebrew)

## 💡 Ideas for Future

- [ ] Integration with synagogue management systems
- [ ] Display community announcements
- [ ] Minyan times display
- [ ] Rabbi's message/thought of the week
- [ ] Photo slideshow mode for special events
- [ ] QR codes for downloading synagogue app
- [ ] Weather display
- [ ] Time until next Shabbat countdown

---

## Notes

- This is a living document - items will be added, removed, and reprioritized as the project evolves
- Items marked with ✓ are completed and will be moved to a CHANGELOG.md
- Priority levels are relative and subject to change based on user feedback and community needs

---

**Last Updated**: February 19, 2026
