1. **Every tool in active use has a named internal owner** — a tool nobody owns is a tool nobody can retire, renew, or negotiate responsibly.
   <!-- source: starter(tech-inventory) -->
   **Status**: adopting — enforced going forward once `core.tech.has_owner_per_tool` is true; existing unowned tools are named, not hidden, while ownership is assigned

2. **Every tool records its actual recurring cost** — an inventory that can't total spend isn't answering the question a tech inventory exists to answer.
   <!-- source: starter(tech-inventory) -->
   **Status**: enforced

3. **Every tool is marked core-operational or convenience** — a Bold in a Day prioritizes fixing or replacing core-operational gaps first; convenience tools are lower-urgency by definition, not absent from the inventory.
   <!-- source: starter(tech-inventory) -->
   **Status**: enforced

4. **Redundant tools — two or more serving the same job — are named explicitly, not left implicit** — the inventory's highest-value output is what to cut, and a redundancy nobody wrote down doesn't get cut.
   <!-- source: starter(tech-inventory) -->
   **Status**: enforced

5. **The inventory is reviewed on the stated cadence** (`core.tech.review_cadence`) — an inventory built once and never revisited is a snapshot, not a working document.
   <!-- source: starter(tech-inventory) -->
   **Status**: enforced
