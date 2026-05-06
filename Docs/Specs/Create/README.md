# Create Spec Notes

Purpose: short, source-backed behavior notes for specific Create blocks and items used by this Unity project.

Rules:

- One file per block or item.
- Keep facts, Unity decisions, and deferred work separate.
- When available, include a short `Ponder Notes` section for the player-facing mental model Create is explicitly teaching.
- Do not write guesses. If something is unclear, mark it as deferred or unknown and add the source anchor that needs follow-up.
- Before changing gameplay for a covered block or item, read its spec first. If source study changes the understanding, update the spec before or with the code change.
- These files are a fast contract for future agents. They do not replace the underlying Minecraft/Create source when deeper implementation detail is needed.

Current first-slice coverage:

- Blocks: `creative_crate`, `brass_funnel`, `item_vault`, `shaft`, `creative_motor`, `belt`
- Items: `andesite_alloy`, `belt_connector`
