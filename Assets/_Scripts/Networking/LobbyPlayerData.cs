using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// Per-player data that is synchronised across all clients via LobbyManager.PlayerList.
/// Must be INetworkSerializable so Netcode can replicate it in a NetworkList.
/// </summary>
public struct LobbyPlayerData : INetworkSerializable, System.IEquatable<LobbyPlayerData>
{
    public ulong               ClientId;
    public FixedString64Bytes  DisplayName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref DisplayName);
    }

    public bool Equals(LobbyPlayerData other) =>
        ClientId == other.ClientId && DisplayName == other.DisplayName;
}
