INCLUDE main/0
INCLUDE main/1/1
EXTERNAL Roll(req,mod)

VAR chapter = 0
VAR scene = ""

VAR ana_health = 30
VAR ana_healthMax = 30
VAR ana_skill = 3
VAR ana_intellect = 3
VAR ana_grace = 3

// Reputation: r_X_Y
//   X = affection (a) / respect (r) / trust (t)
//   Y = entity

VAR r_a_jadar = 0
VAR r_r_jadar = 0
VAR r_t_jadar = 0

-> m_1 ->

-> DONE

=== function Roll(req,mod) ===
    // ~ temp roll = RANDOM(1,20)
    // ~ return roll+mod
    ~ return 1