using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

public class AuthHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputFieldName;
    [SerializeField] private TMP_InputField inputFieldPassword;

    [SerializeField] private GameObject PanelAuth;
    [SerializeField] private GameObject logPanel;
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private GameObject scoreTablePanel;
    [SerializeField] private GameObject pausePanel;

    [SerializeField] private TextMeshProUGUI scoreTxt;
    [SerializeField] private TextMeshProUGUI CurrentScoreTxt;
    [SerializeField] private TextMeshProUGUI playerName;
    
    [SerializeField] private TextMeshProUGUI[] namePlayersTxt;
    [SerializeField] private TextMeshProUGUI[] scorePlayersTxt;

    string url = "https://sid-restapi.onrender.com/";

    private string Username;
    private string Token;

    private int scoreCounter { get; set; }
    private int recordPlayer = 0;

    InputAction tab;
    InputAction escape;

    public int Score
    {
        get => scoreCounter;
        set => scoreCounter += value;
    }

    public void Start()
    {
        Time.timeScale = 0f;
        tab = InputSystem.actions.FindAction("Player/ScoreTable");
        escape = InputSystem.actions.FindAction("Player/Menu");
        tab.Enable();
        escape.Enable();

        Token = PlayerPrefs.GetString("token");
        Username = PlayerPrefs.GetString("username");

        if(string.IsNullOrEmpty(Token) || string.IsNullOrEmpty(Username)) 
        {
            Debug.Log("no hay token");
        }
        else
        {
            StartCoroutine(GetProfile());
        }
    }

    public void Update()
    {
        scoreTxt.text = recordPlayer.ToString();
        CurrentScoreTxt.text = scoreCounter.ToString();

        ScoreTable();

        if (escape.WasPerformedThisFrame()) Pause();
    }

    public void LogIn()
    {
        Credentials credentials = new Credentials();

        credentials.username = inputFieldName.text;
        credentials.password = inputFieldPassword.text;

        string postData = JsonUtility.ToJson(credentials);

        StartCoroutine(LoginPost(postData));
    }

    IEnumerator LoginPost(string data)
    {
        string path = url + "api/auth/login";
        UnityWebRequest www = UnityWebRequest.Put(path, data);
        www.method = "POST";
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(www.error);
        }
        else
        {
            if (www.responseCode == 200)
            {
                string json = www.downloadHandler.text;
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(json);

                Debug.Log(json);

                PlayerPrefs.SetString("token", response.token);
                PlayerPrefs.SetString("username", response.usuario.username);

                Username = response.usuario.username;

                logPanel.SetActive(false);
                welcomePanel.SetActive(true);
                playerName.text = PlayerPrefs.GetString("username");

                Debug.Log("Logged");
            }
            else
            {
                Debug.Log($"Status: {www.responseCode}, Error: {www.error}");
            }
        }
    }

    public void Register()
    {
        Credentials credentials = new Credentials();

        credentials.username = inputFieldName.text;
        credentials.password = inputFieldPassword.text;

        string postData = JsonUtility.ToJson(credentials);

        StartCoroutine(RegisterPost(postData));
    }

    IEnumerator RegisterPost(string data)
    {
        string path = url + "api/usuarios";
        UnityWebRequest www = UnityWebRequest.Put(path, data);
        www.method = "POST";
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(www.error);
        }
        else
        {
            if (www.responseCode == 200)
            {
                Debug.Log(www.downloadHandler.text);

                StartCoroutine(LoginPost(data));
            }
            else
            {
                Debug.Log($"Status: {www.responseCode}, Error: {www.error}");
            }
        }
    }

    IEnumerator GetProfile()
    {
        string path = url + "api/usuarios/"+Username;
        UnityWebRequest www = UnityWebRequest.Get(path);
        www.SetRequestHeader("x-token", Token);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(www.error);
        }
        else
        {
            if (www.responseCode == 200)
            {
                string json = www.downloadHandler.text;
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(json);
                UpdateScore playerDataScore = JsonUtility.FromJson<UpdateScore>(json);
                Debug.Log(json);

                recordPlayer = playerDataScore.data.score;

                welcomePanel.SetActive(true);
                logPanel.SetActive(false);

                playerName.text = PlayerPrefs.GetString("username");
                Debug.Log("Hay token");
            }
            else
            {
                Debug.Log($"Token vencido");
            }
        }
    }

    IEnumerator ListarUsuarios(int limit, int skip, bool sort)
    {
        string path = $"{url}api/usuarios?{limit}={limit}&skip={skip}&sort={sort}";
        UnityWebRequest www = UnityWebRequest.Get(path);
        www.SetRequestHeader("x-token", Token);
        
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(www.error);
        }
        else
        {
            if (www.responseCode == 200)
            {
                string json = www.downloadHandler.text;

                UsuariosResponse response = JsonUtility.FromJson<UsuariosResponse>(json);

                UpdateScore[] leaderboard = response.usuarios
                             .OrderByDescending(u => u.data.score).ToArray();

                for (int j = 0; j < response.usuarios.Count; j++)
                {
                    if (response.usuarios[j].username == Username)
                    {
                        recordPlayer = response.usuarios[j].data.score > scoreCounter ? response.usuarios[j].data.score : scoreCounter;
                    }
                }

                for (int j = 0; j < 4; j++)
                {
                    namePlayersTxt[j].text = leaderboard[j].username;
                    scorePlayersTxt[j].text = leaderboard[j].data.score.ToString();
                }
            }
            else
            {
                Debug.Log($"Token vencido");
            }
        }
    }

    public void ActualizarData()
    {
        UpdateScore credentials = new UpdateScore();

        credentials.username = Username;
        credentials.data.score = recordPlayer;

        string postData = JsonUtility.ToJson(credentials);

        StartCoroutine(UpdateData(postData));
    }

    IEnumerator UpdateData(string data)
    {
        string path = url + "api/usuarios";
        UnityWebRequest www = UnityWebRequest.Put(path, data);
        www.method = "PATCH";
        www.SetRequestHeader("x-token", Token);
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log(www.error);
        }
        else
        {
            if (www.responseCode == 200)
            {
                string json = www.downloadHandler.text;
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(json);

                Debug.Log(json);
            }
            else
            {
                Debug.Log($"Status: {www.responseCode}, Error: {www.downloadHandler.text}");
            }
        }
    }

    public void OnPlay()
    {
        Time.timeScale = 1.0f;
        PanelAuth.SetActive(false);
        welcomePanel.SetActive(false);
    }

    public void ScoreTable()
    {

        if(tab.IsPressed())
        {
            if(!scoreTablePanel.activeSelf)
            {
                StartCoroutine(ListarUsuarios(5, 0, true));
                scoreTablePanel.SetActive(true);
            }
        }
        else
        {
            scoreTablePanel.SetActive(false);
        }
    }

    public void Pause()
    {
        if(!pausePanel.activeInHierarchy)
        {
            Time.timeScale = 0f;

            PanelAuth.SetActive(true);
            pausePanel.SetActive(true);
            welcomePanel.SetActive(false);
            logPanel.SetActive(false);
        }
        else
        {
            Time.timeScale = 1.0f;

            PanelAuth.SetActive(false);
            pausePanel.SetActive(false);
        }
    }

    public void SignOut()
    {
        scoreCounter = 0;

        pausePanel.SetActive(false);
        logPanel.SetActive(true);
    }

    [System.Serializable]
    public class Credentials
    {
        public string username;
        public string password;
    }

    [System.Serializable]
    public class AuthResponse
    {
        public UserModel usuario;
        public string token;
    }

    [System.Serializable]
    public class UserModel
    {
        public string _id;
        public string username;
        public bool estado;
    }

    [System.Serializable]
    public class UsuariosResponse
    {
        public List<UpdateScore> usuarios;
    }

    [System.Serializable]
    public class UpdateScore
    {
        public string username;
        public DataUser data;
        public UpdateScore ()
        {
            data = new DataUser();
        }
    }

    [System.Serializable]
    public class DataUser
    {
        public int score;
    }
}
