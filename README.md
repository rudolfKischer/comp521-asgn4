
# Assignment 4

- Rudolf C. Kischer
- 260956107


# Minotaur AI Design
- The minotaur is a reactive AI with multiple states that it transistions between based on the state of the game.
    - **Gaurd**:
        - When not actively doing anything, the minotaur will enter the gaurd state and will pace in circles around the tresure
    - **Pursue**: 
        - When certain conditions are met , the minotaur will enter the pursue state and select a player to Pursue.
        - Conditions in order of priority:
            - Must not be on a target change cooldown
            - Someone is actively carrying the treasure
            - Someone has recently attacked the minotaur
            - A player is within a certain distance of the minotaur
            - A player is within a certain distance of the treasure
        - If any of the previous conditions are met, it selects the corresponding player to pursue
    - **Attack**:
        - When the minotaur is within a certain distance of a player, it will enter the attack state and attack the player
        - It will activate the area attck, which has a short warmp up and coold down

# World Vector
- Atrributes:
    - PlayerHealth: float
        - (the current health of the player)
    - distanceFromMinotaur: float 
        - (the distance from the minotaur to the player)
    - distanceFromTreasure: float
        - (the distance from the treasure to the player)
    - distanceFromSpawn/Corners: float
        - (the distance from the spawn/corner to the player)
    - IsTreasureCurrentlyCarried: bool
        - (is the treasure currently being carried by a player)
    - MinotaurIsVisible: bool
        - (is the minotaur currently visible to the player)
    - IsFacingMinotaur: bool
        - (is the player currently facing the minotaur)
    - IsRanged: bool
        - (is the player a ranged player)
    - IsTreasureSeekerAssigned: bool
        - (is the player assigned to seek the treasure)
    - IsPlayerSeeker: bool
        - (is the player currently seeking treasure)
    - IsPlayerCarryingTreasure: bool
        - (is the player currently carrying the treasure)
    - IsPlayerOnAttackCooldown: bool
        - (is the player currently on attack cooldown)
    - IsPlayerOnTreasurePickupCooldown: bool
        - (is the player currently on treasure pickup cooldown)
    - IsPlayerDead: bool
        - (is the player currently dead)

# Cooperation

- There is a global variable that is used to assign one player the responsibliity of seeking the treasure
- If someone is already seeking the treasure, then the player will be assigned the responsibliliy of distracting the minotaur
- If the treasure seeker dies, then the when other players ask for a new plan, one of them will be able to pick up the task of treasure seeking
- If the treasure seeker is dropped the treasure or took damage, then because they are on cool down, they will not pick up the responsibliity of treasure seeking until the cool down is over
- This allows other players to attempt to pick up the treasure and seek it if the treasure seeker is dead or on cool down, so it behaves like active hot swapping of the treasure seeker role

