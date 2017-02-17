INCLUDE MQ_00
INCLUDE Prologue
INCLUDE MQ_01
INCLUDE MQ_02
INCLUDE MQ_03
INCLUDE MQ_04
INCLUDE MQ_05
INCLUDE RI_00
INCLUDE Endings
INCLUDE Fragments
INCLUDE Choices_GOE
INCLUDE Choices_ELI
INCLUDE Choices_DEP
INCLUDE Battle
EXTERNAL getReputation(name)

#title: Descent

VAR bgm = "" // Music playing.
VAR room = "" // Room, with all the atmosphere entailed.
VAR posX = 0
VAR posY = 0
VAR posZ = 0
VAR faded = false // Is the screen faded to black or no?
VAR state = "ADV" // ADV, NVL, GAME

VAR day = 0

VAR health = 100
VAR max_health = 100

VAR grace = 1
VAR intellect = 1
VAR skill = 1
VAR lore = 0

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

VAR knows_ritakilledagatha = false


-> DemoIntro

=== Hub
#gameplay
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


== DemoIntro 

Welcome to the demo for _Descent_.

-> choices

= choices
Choose a basic archetype for your character.

+ [Independent]Your character left their home often to explore the city, and wound up getting involved in the odd adventure before our story takes place. They gained some extra experience in the process, but also made some mistakes that could come up later. 
    Does this work for you?
    ** [Yes.]
        ~study = "sword"
        ~skill = 5
        ~intellect = 2
        ~grace = 2
        ~prologue_assailantskilled = 2
        ~prologue_lostear = false
        ~prologue_toldrita = true
        ~prologue_jez_met = true
        ~prologue_jez_acceptedhelp = false
        ~prologue_jez_beaten = true
        ~prologue_wenttoarvvurdeve = false
    ** [No.]
        -> choices
+ [Sheltered]Your character stayed at home most of their life. Because of this, they're in perfect health, and their reputation is practically a blank slate. They also learned a lot from their mother over the years, so while their experience is limited, they have plenty of skills to work with. 
    Does this work for you?
    ** [Yes.]
        ~study = "machinery"
        ~skill = 3
        ~intellect = 5
        ~grace = 1
        ~prologue_assailantskilled = 1
        ~prologue_lostear = false
        ~prologue_toldrita = false
        ~prologue_jez_met = false
        ~prologue_jez_beaten = false
        ~prologue_wenttoarvvurdeve = false
    ** [No.]
        -> choices
+ [Religious]Your character left their home often to spend time with the local priests. They've thus become seasoned in religious lore, and have earned a sparkling reputation with their spiritual leaders. They do, however, have less in the way of skills and experience.
    Does this work for you?
    ** [Yes.]
        ~skill = 1
        ~intellect = 3
        ~grace = 5
        ~study = "art"
        ~prologue_assailantskilled = 0
        ~prologue_lostear = true
        ~prologue_toldrita = true
        ~prologue_jez_met = false
        ~prologue_jez_beaten = false
        ~prologue_wenttoarvvurdeve = true
    ** [No.]
        -> choices

-
-> MQ_01

=== function changeScene(scene) ===
    VAR description = ""
    ~return "[{scene}]"

=== function describeTime(_day) ===
    ~_day = _day mod 1
    {
        -_day<0.1:
            ~return "night"
        -_day<0.3:
            ~return "morning"
        -_day<0.5:
            ~return "early afternoon"
        -_day<0.7:
            ~return "late afternoon"
        -_day<0.9:
            ~return "evening"
        -else:
            ~return "night"
    }


=== function getReputation(name) ===
    ~ return 0
