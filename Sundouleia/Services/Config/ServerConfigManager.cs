using CkCommons;
using Sundouleia.PlayerClient;
using Sundouleia.Services.Mediator;
using SundouleiaAPI.Network;
using System.Diagnostics.CodeAnalysis;

namespace Sundouleia.Services.Configs;

/// <summary> 
///     Config Management for all Server related configs in one, including
///     helper methods to make interfacing with config data easier.
/// </summary>
public class ServerConfigManager
{
    private readonly ILogger<ServerConfigManager> _logger;
    private readonly SundouleiaMediator _mediator;
    private readonly AccountConfig _serverConfig;
    private readonly NickConfig _nickConfig;

    public ServerConfigManager(ILogger<ServerConfigManager> logger, SundouleiaMediator mediator,
        AccountConfig serverConfig, NickConfig nicksConfig)
    {
        _logger = logger;
        _mediator = mediator;
        _serverConfig = serverConfig;
        _nickConfig = nicksConfig;

        _serverConfig.Load();
        _nickConfig.Load();
    }

    /// <summary>
    ///     The current server we are connected to, taken from the ServerStorage class
    /// </summary>
    public AccountStorage ServerStorage => _serverConfig.Current;
    public NickStorage NickStorage => _nickConfig.Current;

    /// <summary>
    ///     Returns the authentication for the current player.
    /// </summary>
    /// <returns> Returns true if found, false if not. Outputs Authentication if true. </returns>
    public bool TryGetAuthForCharacter([NotNullWhen(true)] out Authentication auth)
    {
        // fetch the players local content ID (matches regardless of name or world change) and the name & worldId.
        var LocalContentID = Svc.Framework.RunOnFrameworkThread(() => PlayerData.ContentId).Result;
        // Once we have obtained the information, check to see if the currently logged in character has a matching authentication with the same local content ID.
        auth = ServerStorage.Authentications.FirstOrDefault(f => f.CharacterPlayerContentId == LocalContentID)!;
        if (auth is null)
        {
            _logger.LogDebug("No authentication found for the current character.");
            return false;
        }

        // update the auth for name and world change
        UpdateAuthForNameAndWorldChange(LocalContentID);

        // Return value authentication.
        return true;
    }


    /// <summary>
    ///     Retrieves the SecretKey for the currently logged in character.
    /// </summary>
    public string? GetSecretKeyForCharacter()
    {
        if(TryGetAuthForCharacter(out var auth))
            return auth.SecretKey.Key;

        _logger.LogDebug("No authentication found for the current character.");
        return null;
    }

    public void UpdateAuthForNameAndWorldChange(ulong localContentId)
    {
        // locate the auth with the matching local content ID, and update the name and world if they do not match.
        var auth = ServerStorage.Authentications.Find(f => f.CharacterPlayerContentId == localContentId);
        if (auth == null) return;

        // fetch the players name and world ID.
        var charaName = PlayerData.NameInstanced;
        var worldId = PlayerData.HomeWorldIdInstanced;

        // update the name if it has changed.
        if (auth.CharacterName != charaName)
            auth.CharacterName = charaName;

        // update the world ID if it has changed.
        if (auth.WorldId != worldId)
            auth.WorldId = worldId;
    }

    public int AuthCount() => ServerStorage.Authentications.Count;

    public bool HasAnyAltAuths() => ServerStorage.Authentications.Any(a => !a.IsPrimary);

    public bool CharacterHasSecretKey()
    {
        return ServerStorage.Authentications.Any(a => a.CharacterPlayerContentId == PlayerData.ContentId && !string.IsNullOrEmpty(a.SecretKey.Key));
    }

    public bool AuthExistsForCurrentLocalContentId()
    {
        return ServerStorage.Authentications.Any(a => a.CharacterPlayerContentId == PlayerData.ContentId);
    }

    public void GenerateAuthForCurrentCharacter()
    {
        _logger.LogDebug("Generating new auth for current character");
        // generates a new auth object for the list of authentications with no secret key.
        var auth = new Authentication
        {
            CharacterPlayerContentId = PlayerData.ContendIdInstanced,
            CharacterName = PlayerData.NameInstanced,
            WorldId = PlayerData.HomeWorldIdInstanced,
            IsPrimary = !ServerStorage.Authentications.Any(),
            SecretKey = new SecretKey()
        };

        // add the new authentication to the list of authentications.
        ServerStorage.Authentications.Add(auth);
        Save();
    }

    public void SetSecretKeyForCharacter(ulong localContentID, SecretKey keyToAdd)
    {
        // Check if the currently logged-in character has a matching authentication with the same local content ID.
        var auth = ServerStorage.Authentications.Find(f => f.CharacterPlayerContentId == localContentID);

        // If the authentication is null, throw an exception.
        if (auth == null) throw new Exception("No authentication found for the current character.");

        // Update the existing authentication with the new secret key.
        auth.SecretKey = keyToAdd;
        // Save the updated configuration.
        Save();
    }

    /// <summary> Updates the authentication after successful connection to set the linked UID or flag good connection. </summary>
    /// <remarks> This will also remove any listed accounts that have the 1.3 format and whose UID is not in the connection list. </remarks>
    public void UpdateAuthentication(string secretKey, ConnectionResponse connectedInfo)
    {
        // Firstly, make sure that we have a valid authentication that just connected.
        var auth = ServerStorage.Authentications.Find(f => f.SecretKey.Key == secretKey);
        if (auth is null)
            return;

        // If valid, take this auth and update it with its respective information.
        auth.SecretKey.HasHadSuccessfulConnection = true;
        auth.SecretKey.LinkedProfileUID = connectedInfo.User.UID;
        _logger.LogDebug($"Updating authentication for {auth.CharacterName} with UID {connectedInfo.User.UID}");

        // Now, we should iterate through each of our authentications.
        foreach (var authentication in ServerStorage.Authentications)
        {
            // If the LinkedProfileUID is has a UID listed, but it's not in the list of auth UID's, remove the authentication.
            if (!string.IsNullOrWhiteSpace(authentication.SecretKey.LinkedProfileUID))
            {
                if (!connectedInfo.ActiveAccountUidList.Contains(authentication.SecretKey.LinkedProfileUID))
                {
                    ServerStorage.Authentications.Remove(authentication);
                    continue;
                }
            }
        }
        Save();
    }
 
    /// <summary> Checks if the configuration is valid </summary>
    /// <returns> True if the current server storage object is not null </returns>
    public bool HasValidConfig() => ServerStorage != null;

    /// <summary> Requests to save the configuration service file to the clients computer. </summary>
    public void Save()
    {
        var caller = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name ?? "Unknown";
        _logger.LogDebug("{caller} Calling config save", caller);
        _serverConfig.Save();
    }

    /// <summary> Saves the nicknames config </summary>
    internal void SaveNicknames() => _nickConfig.Save();

    /// <summary>Retrieves the nickname associated with a given UID (User Identifier).</summary>
    /// <returns>Returns the nickname as a string if found; otherwise, returns null.</returns>
    internal string? GetNicknameForUid(string uid)
    {
        if (NickStorage.Nicknames.TryGetValue(uid, out var nickname))
        {
            if (string.IsNullOrEmpty(nickname))
                return null;
            // Return the found nickname
            return nickname;
        }
        return null;
    }


    /// <summary> Set a nickname for a user identifier. </summary>
    /// <param name="uid">the user identifier</param>
    /// <param name="nickname">the nickname to add</param>
    internal void SetNicknameForUid(string uid, string nickname)
    {
        if (string.IsNullOrEmpty(uid))
            return;

        NickStorage.Nicknames[uid] = nickname;
        _nickConfig.Save();
    }
}
