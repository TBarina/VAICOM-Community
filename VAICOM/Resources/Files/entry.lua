local self_ID = "VAICOM"

declare_plugin(self_ID,
{
	installed	 = true,
	dirName		 = current_mod_path,
	binaries	 = {'VAICOM.dll'},

	displayName	 = "VAICOM",
	shortName	 = "VAICOM ",
	fileMenuName = "VAICOM ",

	version		 = "3.0.0",
	state		 = "installed", 	
	developerName= "VAICOM Community",
	info		 = _("VAICOM Community Edition is a voice communications interface plugin for VoiceAttack, enabling true-to-life radio communications with all AI units in the mission."),

	Skins	=
	{
		{
			name	= "VAICOM",
			dir		= "Theme"
		},
	},

	Options =
	{
		{
			name		= "VAICOM",
			nameId		= "VAICOM",
			dir			= "Options",
			CLSID		= "{VAICOM options}"
		},
	},
})

plugin_done()
