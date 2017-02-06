INCLUDE MQ_00
INCLUDE MQ_01
INCLUDE MQ_02
INCLUDE MQ_03
INCLUDE MQ_04
INCLUDE MQ_05
INCLUDE Endings
INCLUDE Prologue
INCLUDE RI_00
INCLUDE Fragments
EXTERNAL getReputation(name)

VAR bgm = "" // Music playing.
VAR room = "" // Room, with all the atmosphere entailed.
VAR posX = 0
VAR posY = 0
VAR posZ = 0
VAR faded = false // Is the screen faded to black or no?
VAR state = "ADV" // ADV, NVL, GAME

VAR health = 100
VAR max_health = 100

// Player traits -- used for calculating reputations
VAR kindness = 0
VAR honesty = 0
VAR pragmatism = 0
VAR pacifism = 0
VAR cleverness = 0
VAR directness = 0
VAR power = 0
VAR selflessness = 0
VAR endurance = 0
VAR humility = 0
VAR piety = 0
    
VAR people_killed = 44

VAR mq04_lostfoldeve = false


-> MQ_00

=== Hub
[Hub]
What would you like to do next?
+ [Talk to Rita] -> RI_00
* {MQ_01} [New Data] -> MQ_02
* {MQ_02} [Conclave ae Scolia] -> MQ_03
* {MQ_03} [Cascade Failure] -> MQ_04
* {MQ_04} [Cocktails and Ruby Slippers] -> MQ_05
* {MQ_05} [Wading in Deeper] -> MQ_06
* {MQ_06 or (MQ_04 and mq04_lostfoldeve)} [Stripping the Veil] -> MQ_07
* -> MQ_END

= MQ_06
[Wading in Deeper]
-> Hub

= MQ_07
[Stripping the Veil]
// The Thing is in Arv'vurdeve, specifically underground, in a massive lab used by the Delgorens to study Progenitor technology. There they decide what, if anything, would be adapted for use in their society. Some is used only in a limited capacity, like 'tablets' (i.e. smartphones), which are reserved only for law enforcement.
-> MQ_08

= MQ_08
[The Goddess of Embers]
-> Hub

= MQ_END
[Everybody Wants to Rule the World]
End.
-> DONE

=== RI_01
Bleem.
-> Hub

=== MV_01
Bleem.
-> Hub

=== WR_01
Bleem.
-> Hub

=== DM_01
Bleem.
-> Hub


=== function getReputation(name) ===
~ return 0
