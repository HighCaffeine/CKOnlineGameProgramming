using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuGUI : MonoBehaviour, IPacketReceiver
{
    void Awake()
    {
        Client.Start();
    }

    // Start is called before the first frame update
    void Start()
    {
        Client.TCP.AddPacketReceiver(this);
    }

    public unsafe void OnPacketReceived(Packet packet)
    {
        ushort packetId = packet.pbase.packet_id;
        switch ((E_PACKET)packetId)
        {
            case E_PACKET.LOGIN_RESPONSE:
                P_LoginRes loginRes = UnsafeCode.ByteArrayToStructure<P_LoginRes>(packet.data);

                string inputName = GameObject.Find("NameInput").GetComponent<InputField>().text;

                LocalPlayerInfo.ID = loginRes.result;
                LocalPlayerInfo.Name = inputName;
                SceneManager.LoadSceneAsync("Scenes/MatchScene", LoadSceneMode.Single);
                break;
        }
    }

    public void OnDestroy()
    {
        Client.TCP.RemovePacketReceiver(this);
    }

    public void OnJoinButtonClick()
    {
        string inputName = GameObject.Find("NameInput").GetComponent<InputField>().text;
        P_LoginReq loginReq = default;
        loginReq.userID = inputName;
        loginReq.userPW = inputName;
        Client.TCP.SendPacket2(E_PACKET.LOGIN_REQUEST, loginReq);
    }
}
