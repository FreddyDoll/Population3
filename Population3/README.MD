# Population 3

Population3 is a C# game project using MonoGame that simulates the early universe with gas grids, particle systems, and procedural generation. Inspired by astrophysics and a touch of sci-fi flair, this project is part of the Population series, exploring the evolution of dark matter entities and cosmic phenomena. 🚀

# ToDos

- Implement long range physics. Field and Particles
- Burning of stars and Collapse
	- Collapse into first Protostar	
	- Ignition! at mass/temperature
	- Star Pressure machanic to stop gaining of mass
	- Collapse at given condition
- Rah Representation:
	- Rah as field: 
		- over a certain density rah become sentient.
			- change in Gas Visualisation (Color)
		- They instantly try to increase their local density to be safe.
		- They can now manipulate their Sphere of influence.
			- Mark Particles and select target density.
				- Player can only influence area around particles
			- Choose to attract more/less mass.
		- if density falls under value, cell is not controlled anymore (neutral). Target Density jumps to base value
		- Simple target density. Higher for sentient then Neutral
			- Pressure mechanic
			- Cells controlled by Rah can adjust their gas constant (equiliubrium density) by adjusting gas constant of the cell
- Sound
	- Background music
	- Soundeffects
		- Protostar forming
		- Ignition
		- Burning
		- Collapse
- Graphics
	- Text Overlay
		- In Game	
		- Cutscene
	- Star shader
		- Instead of Texture
	- Gas a shader
		- Illuminate gas from Stars 
	
## Maybe?

-Move Simulation to GPU

# Issues

- Freezing of particles after merges
- Simulation looks ok but does not scale too well

# GameLoop

- **0** Intro (Cutscene): "From Uniform Field to first stars"
	- Higher Time Step
	- Text Overlay
		- We first remember the time of Uniformity -> Fade from Black to Gray
		- The time of birth but also dispear. -> Rah Sphere of influence
		- Conciousness with nothing to experience.
		- But the cosmos changed. -> Field differences
		- Unnoticably first but quickly accelerating -> especially contracting around rah
		- New things to learn. -> Protostar
		- And the birth of something else -> Ignition around rah star
		- The cosmos was full of experiences -> zoom out, other stars igniting
- **1** "Controling the stars"
	- Player Input:
		- "Manipulate target density around Protostars to adjust absorption rate"
		- Mark star (Select with left stick). Can only select star within Rah Sphere of influence -> only one for first star. 
		- Adjust Slider (LT RT)
			- Left: Donut
			- Center: Even density
			- Right: Blob
		- When selecting flying neutron star (with rah stuck to it -> not possible for first star).
			- Player can hold A and select direction with left stick.
			- When released rah can shoot themself onto new Protostar.
	- Mass gain of stars can only be influenced for certain time (effect weakens after ignition of star)
	- Stars burn out and Collapse -> **2** 
	- No more gas to form stars -> **You loose**
- **2** "Collapse of star":
	- Heavy reduction in Time Step
	- Player Input:
		- chooses direction of shot: Right stick
    - black hole -> **You loose**
	- White Dwarf -> "Keep Rah but no gain" -> **1**
	- Gaining Condition: Neutron Star -> "Shoot Neutron Star to reach new gas clouds" -> **1**
		- Rah are stuck to the flying core

# Background
"Population 0, Das Leben der Rah" revolves around a unique species called the Rah, which exist as dark matter and influence the universe through physical fields. They are the explanation for dark matter. Their evolutionary journey spans several stages:

Population III: The Rah initially manipulate stars, influencing them through their own gravity.
Population 0: At this stage, they progress to biological integration and the creation of megastructures.

Key Concepts
Physics Breakthroughs: The Rah’s abilities break from known physics, particularly in areas like gravity manipulation, electromagnetic control, and information processing.
Extension of the Standard Model: Your idea explores a hypothetical expansion of physics, incorporating dark matter as an active participant rather than a passive element.
[Jippity]

# DONE
-DONE: Get The Gas To Collapse and form a star