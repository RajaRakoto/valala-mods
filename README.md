# VALALA Mods

Personal mod built specifically to tune and personalize our dedicated **VALALA** servers.
It is released publicly so other players can reproduce the same setup.

Made with ⚡ by Raja from Madagascar 🇲🇬

**Keywords:** Valala, Dedicated Server, Mods, Deep North Update, Tweaks

## Features

**VALALA server tuning**

- Selectable **element classes**: Lightning, Rock, Fire, Poison, Ice.
- In-game **radial selection wheel**, opened with a configurable key (`;` by default).
- Per-element **visual effect**, **damage type** and **native Valheim status effect**.
- Five craftable **Valala silver rings**; the matching ring must be equipped for an
  element to work. Rings upgrade to **quality 4**.
- **Second trinket slot** for every trinket through **ExtraSlotsCustomSlots** (optional).
- **Wind Impulse can be cast without a staff** with any Valala ring quality (`Alt+F`); own charge/jump pipeline, free movement, no charge animation, charge time and jump height scale with quality.
- **Thor landing impact** and **elemental landing impacts** from a configurable fall height.
- **High-fall lightning landing** for Lightning at 20 m or more.
- Damage scales with the vanilla **ElementalMagic** skill.
- Optional **multiplayer sync**: other players see your effects.
- Fully configurable, with **live config reload** (no restart needed).

**Mushoku Tensei integration**

- Adds **elemental classes** to the **Wind Impulse / Wind Boost** dash.
- Each class has its own effect and deals **elemental damage to enemies only**.
- This is **not** a fork or copy of Mushoku Tensei. It only hooks an existing behavior in
  memory and does not modify any file from that mod.

## Requirements

Hard dependencies:

- **BepInExPack for Valheim** `5.4.2351`.
- **Mushoku Tensei Core** (`BrayanRF-MushokuTenseiCore`) `3.19.27`.
- **Jotunn** (`ValheimModding-Jotunn`) `2.30.2`.
- **EpicLoot** (`RandyKnapp-EpicLoot`) `0.14.13`.

Optional for the second trinket slot:

- **ExtraSlots** (`shudnal-ExtraSlots`) `1.2.21`.
- **ExtraSlotsCustomSlots** (`shudnal-ExtraSlotsCustomSlots`) `1.0.23`.

The plugin has hard `BepInDependency` entries on `mushokutensei.magia`,
`com.jotunn.jotunn` and `randyknapp.mods.epicloot`, and soft dependencies on
`shudnal.ExtraSlots` and `shudnal.ExtraSlotsCustomSlots`.

## Installation

Install through Thunderstore Mod Manager / r2modman — dependencies are handled automatically.

**Dedicated servers and clients must install Valala Mods.** The rings are custom items
registered through Jotunn, so the server needs the same prefabs and recipes as the clients.

Manual install: place `ValalaMods.dll` into `BepInEx/plugins/`.

## Configuration

The config file is created automatically on first launch at:

`BepInEx/config/odin.valala.raja.mods.cfg`

| Key | Default | Meaning |
|---|---|---|
| Enabled | true | Master switch |
| Mode | Add | `Add` = wind VFX + element · `Replace` = element only when its ring is equipped · staff-free casts always use the Valala VFX |
| Duration | 1.5 | Lifetime of the strike effect (s) |
| Radius | 0.5 | Radius of the strike visuals and cast damage |
| Height | 1.8 | Elemental VFX height factor |
| Color | 59BFFF | Lightning tint (hex RRGGBB); other elements use their own tint |
| AffectMk1Jump | false | Also trigger the strike on the Mk-I armor jump |
| SyncToOthers | true | Broadcast effects to other players; off = you only |
| MenuKey | `Semicolon` | Key that opens the element wheel |
| SelectedElement | `Foudre` | Active element (`Foudre`, `Roche`, `Feu`, `Poison`, `Glace`) |
| RingDamageBonus | 0.15 | Damage bonus with the matching ring equipped (`0.15` = +15%) |
| RingDamageBonusPerLevel | 0.15 | Extra damage per ring quality above 1 (`0.15` = +15%; quality 4 = +60% total) |
| SecondTrinketSlot | true | Enable the extra trinket slot through ExtraSlotsCustomSlots |
| StaffFreeWindImpulse | true | Allow staff-free Wind Impulse with any Valala ring quality; own charge/jump, free movement |
| StaffFreeCastKey | `Alt+F` | Shortcut for staff-free Wind Impulse; plain `F` remains MT Toki Step |
| LandingBoom | true | Elemental landing impact (requires the matching ring; no light flash for non-Lightning) |
| LandingHeight | 10 | Minimum fall height for the landing impact (m) |
| LandingRadius | 4 | Landing and q3/q4 cast damage radius (m) |
| LandingThunder | true | High-fall lightning VFX + damage (Lightning only) |
| LandingThunderHeight | 20 | Minimum fall height for the high-fall strike (m) |
| LandingThunderRadius | 4 | Damage radius of the high-fall strike (m) |
| Damage | true | Enable enemy damage, including landing damage |
| DamageMin | 8 | Damage at Elemental Magic level 0 |
| DamageMax | 60 | Damage at level 100 |

Old `SelectedElement=Vent` configs are migrated to `Roche` automatically.

### Landing tiers

| Fall height | Effect |
|---|---|
| `< LandingHeight` | Nothing |
| `LandingHeight` to `< LandingThunderHeight` | Elemental landing impact + generic damage |
| `>= LandingThunderHeight`, Lightning | Thor impact **+** high-fall lightning VFX **+** high-fall damage |
| `>= LandingThunderHeight`, other elements | Normal elemental landing impact |

For Lightning, the high-fall damage pulse replaces the generic landing damage pulse to
avoid double-dipping. `LandingThunderHeight` is clamped at runtime to at least
`LandingHeight`.

Non-Lightning landings reuse the sledge circular shockwave (`vfx_sledge_iron_hit`),
tinted to the element, then add an elemental burst (AOE + sparks) and the shared
impact sound. They deliberately spawn **no light flash** and no vertical lightning;
Lightning keeps its existing VFX.

## Element classes

| Class | Damage | Native effect |
|---|---|---|
| Lightning | Lightning | Lightning status |
| Rock | Blunt | Increased stagger |
| Fire | Fire | Burning |
| Poison | Poison | Poison damage over time |
| Ice | Frost | Frost slow |

Damage targets **enemies only**; the game's normal immunity and resistance still apply.
The landing damage also ignores you, other players, tames and traders/NPCs.

## Valala rings

Each element requires its matching ring to be **equipped**. Without the ring, the cast
does nothing and `Mode=Replace` keeps the vanilla wind VFX.

All rings are crafted at a **forge level 2** and are cloned from EpicLoot's `SilverRing`
in memory. No EpicLoot file is modified.

| Ring | Recipe |
|---|---|
| Valala Silver Ring — Lightning | Silver 20, Crystal 10, Thunderstone 2, FineWood 10 |
| Valala Silver Ring — Rock | Silver 20, Stone 50, Crystal 10, FineWood 10 |
| Valala Silver Ring — Fire | Silver 20, Ruby 2, SurtlingCore 5, Coal 20 |
| Valala Silver Ring — Poison | Silver 20, Ooze 10, Bloodbag 5, BoneFragments 20 |
| Valala Silver Ring — Ice | Silver 20, FreezeGland 5, Crystal 10, FineWood 10 |

### Ring quality

Rings use the vanilla quality scale and can be upgraded to **quality 4** at the forge.
The forge must reach level 3 for quality 2, level 4 for quality 3, and level 5 for
quality 4. Each quality adds damage and improves the staff-free Wind Impulse:

| Quality | Charge time | Jump power |
|---|---:|---:|
| 1 | 2.75 s | 2.5× (Mushoku Tensei maximum) |
| 2 | 5.25 s | 3.0× |
| 3 | 7.75 s | 3.5× |
| 4 | 10.25 s | 4.0× (+60% over Mushoku Tensei) |

Charge times assume Mushoku Tensei's default `Channeling/Speed = 0.4`.

At quality **3 and 4**, releasing Wind Impulse uses the landing damage pulse at the
caster's position instead of the small default cast pulse. It uses `LandingRadius`, the
matching element and the same enemy-only filtering as a real landing.

### Second trinket slot

The extra slot is provided by ExtraSlotsCustomSlots, then extended to every item whose
type is `Trinket` (not only Valala rings). Configure the profile once:

`BepInEx/config/shudnal.ExtraSlotsCustomSlots.cfg`

```ini
[General]
Lock Configuration = false

[User defined - Custom slot 1]
Enabled = true
Name = Trinket
Global keys =
Item list =
Item is visible = false
```

Valala Mods replaces the custom slot predicate at runtime to accept all trinkets.
The vanilla trinket slot remains available for a second trinket. No other ExtraSlots
setting needs to be changed. Without ExtraSlotsCustomSlots, the second slot stays
disabled and the rest of the mod works normally.

### Staff-free Wind Impulse

With any **Valala ring** equipped, no staff, and Wind Impulse learned:

- `Alt+F` starts the charge;
- hold `Alt+F` to charge;
- release to cast.

The charge is owned by Valala Mods, not by the Mushoku Tensei cast pipeline:

- the HUD is independent and appears immediately;
- there is **no charge animation or posture**;
- you can charge while walking, running, attacking or blocking;
- the charge is cancelled and mana refunded on death, stagger, swim, encumbered,
  dodge, menus/chat/console/inventory, ring removal, staff equip or another MT cast;
- the jump uses the Valala quality scaling directly (`2.5/3.0/3.5/4.0×`);
- q3/q4 cast damage uses the landing pulse once, without double-dipping;
- the elemental Valala VFX is used, without the Mushoku Tensei wind lines;
- Mushoku Tensei's no-fall-damage status and 6 s speed/jump buff are kept.

The staff Wind Impulse cast remains unchanged.

`Alt+F` is used because plain `F` is Mushoku Tensei's **Tōki Step**. While `Alt+F` is
held for Wind Impulse, Tōki Step is suppressed; pressing `F` alone still dashes.

The charge can be held indefinitely at its maximum; the spell no longer fires by itself.
Charge time and jump power follow the ring quality table above. A small bar appears at
the bottom center of the screen while charging: the fill is the current charge and the
gold marker is the current ring's limit.

The normal cooldown, mana cost, range and elemental effects still apply. If mana runs
out during the charge, the charge stops advancing instead of being cancelled; releasing
still requires enough mana to pay for the spell. The shortcut is configurable in the
config editor; plain `E` is available but conflicts with Valheim's interact key. The
spell does not need to be the selected spell. The magic wheel still requires a staff;
this shortcut is deliberately limited to Wind Impulse.

## Damage

- A single elemental hit per cast at quality 1–2.
- Quality 3–4 replaces it with the landing-style enemy-only damage pulse at cast time.
- Native status effects are applied by the damage type itself.
- Landing adds one enemy-only damage pulse.
- The matching ring adds **+15%** damage by default.
- Damage is **dynamic**, based on the vanilla **ElementalMagic** skill.

## Multiplayer

- The mod must be installed on the **server and all clients**.
- All peers must use the **same Valala Mods version**; the landing RPC signature changed
  in `1.1.0`.
- With `SyncToOthers=true` (default), effects are broadcast so other players see them.
- A headless dedicated server does not render VFX, but it still registers the custom
  rings, recipes and RPC handlers.

## Uninstall

1. Delete the mod's plugin folder under `BepInEx/plugins/`.
2. Delete `BepInEx/config/odin.valala.raja.mods.cfg`.

Fully reversible.

## Notes

- Requires Mushoku Tensei Core, Jotunn and EpicLoot. If one of those mods renames internal
  types, a future update of Valala Mods may be needed; it will not crash the game.
- Uses the game's existing visual effects; no extra assets are shipped.
- Uses EpicLoot's `SilverRing` only as an in-memory clone base.
