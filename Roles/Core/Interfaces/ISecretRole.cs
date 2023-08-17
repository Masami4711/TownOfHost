namespace TownOfHost.Roles.Core.Interfaces;

/// <summary>
/// 追放されたときに誰かを道連れにする役職
/// </summary>
public interface ISecretRole
{
    /// <summary>
    /// 表示置き換え先の役職
    /// </summary>
    public CustomRoles ReplaceRole { get; }
    /// <summary>
    /// 表示置き換えの条件
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    public bool DoReplace(PlayerControl player);
}
