# HBFC

HBFC is a Skyrim SE/AE Synthesis patcher for configurable, hold-specific crime bounty values.

It patches the vanilla crime factions for Skyrim's nine holds, the Orc crime faction, and Raven Rock. Each faction can be configured separately for murder, assault, trespass, pickpocketing, stealing, escaping jail, and werewolf crimes through the normal Synthesis settings UI.

The patcher starts from each faction's winning override, changes only the configured crime values, and clears `Crime Gold - Use Defaults` so those values take effect. All unrelated faction data is preserved.

## Installation

Add the following Git repository as a patcher in Synthesis:

`https://github.com/hoskonen/synthesis-hbfc`

Run HBFC late in your Synthesis group or load order so it can forward the winning faction records safely.
