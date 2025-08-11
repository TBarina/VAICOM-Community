
![Vaicom GitHub Banner](https://github.com/user-attachments/assets/f7522f44-efb5-427e-b99f-868a09429806)


[![Downloads](https://img.shields.io/github/downloads/Penecruz/VAICOMPRO-Community/total?logo=GitHub)](https://github.com/Penecruz/VAICOMPRO-Community/releases/latest)
[![Discord](https://img.shields.io/discord/736032844274728961?logo=Discord)](https://discord.gg/7c22BHNSCS)
[![Latest Release](https://img.shields.io/github/v/release/Penecruz/VAICOMPRO-Community?logo=GitHub)](https://github.com/Penecruz/VAICOMPRO-Community/releases/latest)

VAICOM Community Edition for DCS World

## Overview - Community Edition

On 31 OCT 2022 Hollywood_315 open sourced his awesome AI communications software for DCS Word. VAICOMPRO has been the launch pad for VR flyers in DCS to create a
immersive environment free from the constraints of keyboard or mouse-controlled radio menus.

A group of community members have patched his work to make it compatible with DCS 2.8.XXXXX and later. This is a standalone installer that will replace your previous version of VAICOM. It will not work with DCS 2.7.XXXXX or erlier.

We now have VAICOM Community Edition running well with DCS 2.8.X.X and are looking where we can take it going forward with lots of new modules coming to DCS World.
We continue to develop VAICOMPRO to keep it functioning with changes to DCS. That said, there will be issues from time to time. So please use the issues register here on GitHub to report them.

Remember this is a community group, a group that donates their time to keep this awesome software alive. Be respectful and patient, we all have real jobs too. Join our Discord Server (link Below) and become part of our community.

## Important Information

VAICOM Community is 100% free and includes all modules (Chatter, AIRIO, Kneeboard, Realistic ATC) that were available with the last paid release.

Use of this software is at your risk, we accept no liability for stuffing up your Voice Attack installation, DCS World installation, Windows installation, or any other action.

The VAICOM Community Team

## Known Issues

VAICOM Community 2.8.X.X is not designed to be backwards compatible with DCS 2.7.X If you wish to continue using VAICOMPRO for DCS 2.7, please use Hollywood_315's final release and not VAICOMPRO Community.

VAICOM Community Edition will not pass the Integrity Check on Multiplayer Servers that require Pure Client Scripts unless the AIRIO and Kneeboard extensions are deactivated via the VAICOMPRO UI.
This is because VAICOMPRO adds lines to some of DCS World's core LUA files to enable it to function. Multiplayer Server administrators must enable Pure Client Scripts as an option as it is off by default. Very few Servers require Pure Client Scripts. This is something that only ED can change.

DiCE: DCS Integrated Countermeasure Editor creates many functionality issues with VAICOM Community, and it is recommended this be uninstalled before using VAICOM Community.

Flashing Comms Menu after DCS World update is a known issue and can be resolved with a lua reset, closing DCS and voiceAttack then launching VoiceAttack again prior to launching DCS to generate DCS side files.

## Installation Instructions

#### NOTE: If this is a new VAICOM installation, you should follow the install instructions in the VAICOM manual found in the VAICOMPRO/Documentation folder.
	
#### To update from an older version of VAICOMPRO


1. Ensure DCS is not Running

2. Backup your current VoiceAttack profile by clicking "More Profile Actions" (button right of the edit in VoiceAttack) and exporting your profile to a known location (this avoids tears in the event of an issue).

3. If you are using the MSI Installer, you will need to uninstall via the Windows process It will retain your config and profile settings (You will be propted if you try running the installer)

4. If you are using the Zip file just unzip over the top of you existing VAICOMPRO folder in Program Files/ VoiceAttack /Apps folder

5. Launch VoiceAttack and exit VoiceAttack (this allows VAICOMPRO to build the required DCS files).
	
6. Launch VoiceAttack and launch the VAICOM config menu (L CTRL+L ALT+C) Check that your settings have been retained and the DCS Path details are correct.

7. Launch DCS and confirm 

8. Join our Discord at https://discord.gg/7c22BHNSCS if you have any questions or issues with the install.

## Installation Tutorial Videos

[![IMAGE ALT TEXT HERE](https://img.youtube.com/vi/-bbQf6cU2EM/0.jpg)](https://www.youtube.com/watch?v=-bbQf6cU2EM)

[![IMAGE ALT TEXT HERE](https://img.youtube.com/vi/NiP42guoKW0/0.jpg)](https://www.youtube.com/watch?v=NiP42guoKW0)

[![IMAGE ALT TEXT HERE](https://img.youtube.com/vi/TJjd0Pvccmk/0.jpg)](https://www.youtube.com/watch?v=TJjd0Pvccmk)


## Patch Notes


**VAICOM plugin 3.0.5.3**

This update adds a new hooking method for DCS and a major lua optimization, which brings a faster communication rate between Vaicom and DCS, along with better logging of message construction and message passing when set to debug. It also adds a new file handling routine.

The import part for the end user, It also makes many fixes, brings better Kneeboard data management and stops the phantom Scripts file that was being generated in the root directory during file writing.

-	Fixes the rearming command to ensure the Mission Resources dialog pops up.
-	Fixes NVG, HMD, Inertial starter commands and other special commands.
-	Fixes “request taxi for takeoff” bug
-	Fixes Supercarrier verification bug
-	Fixes lua code reset behavior and expands to cleanup legacy export.lua hook if present.
-	New Kneeboard frequency priority for recipients UHF>VHF>FM.
-	New faster two-way communications rate between DCS and Vaicom.
-	New safe handling of export.lua and better logging.
-	New lua code optimization for many appended files.
-	New Vaicom UI window handling now will not remain on top of other windows.

ALL USERS NEED TO DO A RESET OF THEIR LUA CODE USING THE RESET TAB OF THE VAICOM UI.
If you do not perform this reset the legacy entry will remain and Vaicom will not connect to DCS


Known Issues

- None 

## Community Team

Pene, Special K, D3adCy11nd3r, Folgers, Hornblower793, Liam8, MAXsenna, MisterOutofTime, Raskit, Hue Jass and stag1975

## Patreon Donations

If you want to donate a beer, visit the Official Vaicom Patreon.
[Vaicom Patreon Site](https://patreon.com/PeneCruz?utm_medium=unknown&utm_source=join_link&utm_campaign=creatorshare_creator&utm_content=copyLink)


#### Beta Team
104th_Aeons, GSG-3|Turbine|202, DrChainsaw, Jaeger, Nicola, Padinn, SPAZ-505, tomeye, Virus, Bonz RexExGSR, LawnBoy, Scotia, Sleighzy and MrAxen, 
