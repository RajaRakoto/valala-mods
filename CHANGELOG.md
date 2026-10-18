# Changelog

## 1.3.3

- Staff-free Wind Impulse now uses a Valala-owned charge and jump pipeline instead of the Mushoku Tensei cast pipeline.
- The charge HUD is independent and appears immediately.
- Removed the charge animation and posture: charging works while walking, running, attacking and blocking.
- Charge is cancelled and mana refunded on death, stagger, swim, encumbered, dodge, menu/chat/console/inventory, ring removal, staff equip or another MT cast.
- Jump power now comes directly from Valala quality scaling (`2.5/3.0/3.5/4.0×`) without post-scaling MT `ForceJump`.
- Kept Mushoku Tensei's no-fall-damage status and 6 s speed/jump buff.
- Staff-free release now uses the Valala elemental VFX only, without Mushoku Tensei wind lines.
- q3/q4 cast damage still uses the landing pulse once, without double-dipping.
- Staff Wind Impulse cast remains unchanged.

## 1.3.2

- Ring quality 3 and 4 now use the landing damage pulse for the Wind Impulse cast itself.
- The cast pulse uses `LandingRadius`, the matching element damage and the same enemy-only target rules as landing.
- The small default cast pulse is replaced at quality 3–4, so damage is not doubled.

## 1.3.1

- Fixed staff-free Wind Impulse auto-cancelling after ~1 s through MT attack safety (`AtaqueSeguro` → `Caster.Reiniciar`).
- Staff-free charge now counts as busy for MT attack safety, and `Caster.Reiniciar` is blocked while charging.
- Default shortcut is now `Alt+F`; old `F` configs are migrated automatically.
- Tōki Step is suppressed while `Alt+F` is held for Wind Impulse; `F` alone still dashes.
- Moved the charge bar HUD slightly higher.

## 1.3.0

- Staff-free Wind Impulse now works with **every Valala ring quality**, not only quality 4.
- Charge time and jump power scale with ring quality: 2.75/5.25/7.75/10.25 s and 2.5/3.0/3.5/4.0×.
- Quality 4 jump power is **+60% over Mushoku Tensei's maximum**.
- The charge can now be held **indefinitely** at maximum; release casts the spell.
- Added a **charge bar HUD** with a gold marker at the equipped ring's limit.
- Fixed early charge cancellation; low mana now caps the charge instead of cancelling it.
- No third-party file is modified; all changes are Harmony patches.

## 1.2.0

- Added a **second trinket slot** through ExtraSlotsCustomSlots custom slot 1, extended to every `Trinket` item at runtime.
- Rings now upgrade to **quality 4** with per-level material costs.
- Ring damage now scales per quality: +15% base and +15% per quality above 1 (quality 4 = +60% total).
- Added **staff-free Wind Impulse** at ring quality 4 with a configurable shortcut (`Alt+E` by default, dropdown presets).
- The staff-free cast keeps the normal cooldown, mana cost, charge/hold/release behavior and elemental VFX.
- Added `SecondTrinketSlot`, `StaffFreeWindImpulse`, `StaffFreeCastKey` and `RingDamageBonusPerLevel` config entries.
- Added soft dependencies on ExtraSlots and ExtraSlotsCustomSlots; the rest of the mod works without them.

## 1.1.0

- Replaced the Wind class with **Rock**.
- Added **Poison** and **Ice** element classes. Final set: Lightning, Rock, Fire, Poison, Ice.
- Added five craftable **Valala silver rings** (forge level 2); the matching ring must be equipped.
- Added **+15%** ring damage bonus (configurable).
- Added native damage/status mapping: Rock = blunt + stagger, Poison = poison DoT, Ice = frost slow.
- Added **elemental landing impacts** for all classes. Lightning keeps its high-fall strike.
- Non-Lightning landings reuse the tinted circular shockwave, add an elemental burst, and keep the shared impact sound without a light flash or vertical lightning.
- Added `LandingRadius` and ring damage bonus config entries.
- Migrated old `SelectedElement=Vent` configs to `Roche`.
- The mod now requires **Jotunn** and **EpicLoot**; rings are cloned from EpicLoot's `SilverRing`.
- **Server and clients must install the same version.** The landing RPC now carries the element.

## 1.0.0

- First release of **Valala Mods**.
- Adds **elemental classes** (Lightning, Wind, Fire) to the Wind Impulse / Wind Boost dash.
- In-game **radial selection wheel** with procedural icons and a configurable key.
- Per-element **visual effect** and **enemy-only damage** (Lightning / Fire).
- Damage scales with the vanilla **ElementalMagic** skill.
- Optional **multiplayer sync** of the effects.
- Live **config reload** (key and class applied without restarting).
- **Thor landing impact** for the Lightning class when falling from a configurable height.
- **High-fall lightning landing** at 20 m or more, with the release-style VFX and an
  enemy-only damage pulse. The normal Thor impact still plays below and above it.
- High-fall landing damage ignores the caster, other players, tames and traders/NPCs.
