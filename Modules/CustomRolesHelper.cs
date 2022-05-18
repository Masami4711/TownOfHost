namespace TownOfHost
{
    static class CustomRolesHelper
    {
        public static bool isImpostor(this CustomRoles role)
        {
            return
                role == CustomRoles.Impostor ||
                role == CustomRoles.Shapeshifter ||
                role == CustomRoles.BountyHunter ||
                role == CustomRoles.Vampire ||
                role == CustomRoles.Witch ||
                role == CustomRoles.ShapeMaster ||
                role == CustomRoles.Warlock ||
                role == CustomRoles.SerialKiller ||
                role == CustomRoles.Mare ||
                role == CustomRoles.Puppeteer ||
                role == CustomRoles.EvilWatcher ||
                role == CustomRoles.TimeThief ||
                role == CustomRoles.Mafia ||
                role == CustomRoles.FireWorks ||
                role == CustomRoles.Sniper ||
                role == CustomRoles.Ninja;
        }
        public static bool isMadmate(this CustomRoles role)
        {
            return
                role == CustomRoles.Madmate ||
                role == CustomRoles.SKMadmate ||
                role == CustomRoles.MadGuardian ||
                role == CustomRoles.MadSnitch ||
                role == CustomRoles.MSchrodingerCat;
        }
        public static bool isImpostorTeam(this CustomRoles role) => role.isImpostor() || role.isMadmate();
        public static bool isNeutral(this CustomRoles role)
        {
            return
                role == CustomRoles.Jester ||
                role == CustomRoles.Opportunist ||
                role == CustomRoles.SchrodingerCat ||
                role == CustomRoles.Terrorist ||
                role == CustomRoles.Executioner ||
                role == CustomRoles.Arsonist ||
                role == CustomRoles.Egoist ||
                role == CustomRoles.EgoSchrodingerCat ||
                role == CustomRoles.HASTroll ||
                role == CustomRoles.HASFox;
        }
        public static bool isVanilla(this CustomRoles role)
        {
            return
                role == CustomRoles.Crewmate ||
                role == CustomRoles.Engineer ||
                role == CustomRoles.Scientist ||
                role == CustomRoles.GuardianAngel ||
                role == CustomRoles.Impostor ||
                role == CustomRoles.Shapeshifter;
        }

        public static RoleType getRoleType(this CustomRoles role)
        {
            RoleType type = RoleType.Crewmate;
            if (role.isImpostor()) type = RoleType.Impostor;
            if (role.isNeutral()) type = RoleType.Neutral;
            if (role.isMadmate()) type = RoleType.Madmate;
            return type;
        }
        public static void setCount(this CustomRoles role, int num) => Options.setRoleCount(role, num);
        public static int getCount(this CustomRoles role)
        {
            if (role.isVanilla())
            {
                RoleOptionsData roleOpt = PlayerControl.GameOptions.RoleOptions;
                return role switch
                {
                    CustomRoles.Engineer => roleOpt.GetNumPerGame(RoleTypes.Engineer),
                    CustomRoles.Scientist => roleOpt.GetNumPerGame(RoleTypes.Scientist),
                    CustomRoles.Shapeshifter => roleOpt.GetNumPerGame(RoleTypes.Shapeshifter),
                    CustomRoles.GuardianAngel => roleOpt.GetNumPerGame(RoleTypes.GuardianAngel),
                    CustomRoles.Crewmate => roleOpt.GetNumPerGame(RoleTypes.Crewmate),
                    _ => 0
                };
            }
            else
            {
                return Options.getRoleCount(role);
            }
        }
        public static bool isEnable(this CustomRoles role) => role.getCount() > 0;
    }
    public enum RoleType
    {
        Crewmate,
        Impostor,
        Neutral,
        Madmate
    }
}
