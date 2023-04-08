using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainSystemManager : MonoBehaviourPunCallbacks
{
  [SerializeField] private GameObject[] UIs;
  // User1
  [SerializeField] private GameObject SystemObject;
  [SerializeField] private Text[] SystemTexts;
  [SerializeField] private Dropdown QuestionDropdown;
  [SerializeField] private Toggle IsMultiplayingToggle;
  [SerializeField] private Image PreviewQuestion;
  // User2
  [SerializeField] private Text TimerTextInQuestionDevice;
  [SerializeField] private Text BaseTimerTextInQuestionDevice;
  [SerializeField] private Text QuestionIntroductionText;
  [SerializeField] private Image QuestionImage;
  [SerializeField] private GameObject KeyboardMasterObj;
  [SerializeField] private InputField AnswerFieldText;
  [SerializeField] private GameObject[] GameResultObjectsInQuestionDevice;
  [SerializeField] private GameObject PalseInQuestionDevice;
  // User3
  [SerializeField] private Text TimerTextInOuterTimerDevice;
  [SerializeField] private Text BaseTimerTextInOuterTimerDevice;
  [SerializeField] private GameObject[] GameResultObjectsInOuterTimerDevice;
  [SerializeField] private GameObject PalseInOuterTimerDevice;
  // Quest
  [SerializeField] private QuestionClass[] Questions;
  // Audio
  [SerializeField] private AudioSource Noise;
  [SerializeField] private AudioSource ClockClick;
  [SerializeField] private AudioSource HeavyPiano;

  [Serializable] private class QuestionClass
  {
    public string Id;
    public Sprite Image;
    public string Answer;
  }

  private int userIndex = 0;
  private int questionIndex = 0;
  private string gameMode = "";
  private float timer = 0;
  private Coroutine timerCoroutine;

  private void Start()
  {
    userIndex = int.Parse(PhotonNetwork.NickName[4].ToString());
    switch (userIndex)
    {
      case 1:
        UIs[0].SetActive(true);
        break;
      case 2:
        UIs[1].SetActive(true);
        break;
      case 3:
        UIs[2].SetActive(true);
        break;
    }
  }

  public override void OnPlayerEnteredRoom(Player newPlayer)
  {
    base.OnPlayerEnteredRoom(newPlayer);

    if (PhotonNetwork.PlayerList.Length == 3 && userIndex == 1) SystemObject.SetActive(true);
  }

  public override void OnPlayerLeftRoom(Player otherPlayer)
  {
    base.OnPlayerLeftRoom(otherPlayer);

    if (userIndex == 1) SystemObject.SetActive(false);
  }

  public void OnSelectResetQuestion()
  {
    photonView.RPC(nameof(ResetQuestionRPC), RpcTarget.AllViaServer);
  }

  public void OnSelectDisplayQuestion()
  {
    photonView.RPC(nameof(SetQuestionRPC), RpcTarget.AllViaServer, new object[] { QuestionDropdown.value - 1, IsMultiplayingToggle.isOn });
  }

  public void OnSelectResetTimer()
  {
    photonView.RPC(nameof(ResetTimerRPC), RpcTarget.AllViaServer);
  }

  public void OnSelectStartTimer()
  {
    photonView.RPC(nameof(StartTimerRPC), RpcTarget.AllViaServer);
  }

  public void OnSelectDisconnectOtherDevice()
  {
    photonView.RPC(nameof(Disconnect), RpcTarget.Others);
  }

  public void OnSelectDisconnectAllDevice()
  {
    photonView.RPC(nameof(Disconnect), RpcTarget.AllViaServer);
  }

  public override void OnLeftRoom()
  {
    base.OnLeftRoom();

    PhotonNetwork.LeaveLobby();
    SceneManager.LoadScene("ConnectionScene");
  }

  [PunRPC] public void ResetQuestionRPC()
  {
    if (userIndex == 2)
    {
      QuestionImage.sprite = null;
      GameResultObjectsInQuestionDevice[0].SetActive(false);
      GameResultObjectsInQuestionDevice[1].SetActive(false);
      KeyboardMasterObj.SetActive(false);
      AnswerFieldText.text = "";
      QuestionIntroductionText.text = "";
      TimerTextInQuestionDevice.text = "";
      TimerTextInQuestionDevice.color = Color.white;
      BaseTimerTextInQuestionDevice.text = "88:88";
    }
    else if (userIndex == 1)
    {
      SystemTexts[0].text = SystemTexts[1].text = SystemTexts[2].text = SystemTexts[3].text = "";
      PreviewQuestion.sprite = null;
    }
    else if (userIndex == 3)
    {
      GameResultObjectsInOuterTimerDevice[0].SetActive(false);
      GameResultObjectsInOuterTimerDevice[1].SetActive(false);
      TimerTextInOuterTimerDevice.text = "";
      TimerTextInOuterTimerDevice.color = Color.white;
      BaseTimerTextInOuterTimerDevice.text = "88:88";
    }
  }

  [PunRPC] public void SetQuestionRPC(int index, bool isMultiplay)
  {
    questionIndex = index;
    if (userIndex == 1)
    {
      SystemTexts[0].text = Questions[index].Id;
      if (isMultiplay) SystemTexts[0].text += " +協力プレイ";
      PreviewQuestion.sprite = Questions[index].Image;
    }
    else if (userIndex == 2)
    { 
      QuestionImage.sprite = Questions[index].Image;
      if (isMultiplay) QuestionIntroductionText.text = "仲間と協力して謎を解け。";
      else QuestionIntroductionText.text = "謎の答えを自力で解き明かせ。";

      if (Questions[index].Answer.Contains("1")) AnswerFieldText.placeholder.gameObject.GetComponent<Text>().text = "半角数字で入力";
      else if (Questions[index].Answer.Contains("K")) AnswerFieldText.placeholder.gameObject.GetComponent<Text>().text = "半角アルファベットで入力";
      else AnswerFieldText.placeholder.gameObject.GetComponent<Text>().text = "ひらがなで入力";
    }
  }

  [PunRPC] public void ResetTimerRPC()
  {
    timer = 180;
    switch (userIndex)
    {
      case 1:
        SystemTexts[3].text = "残り 3分 0秒 000";
        break;
      case 2:
        TimerTextInQuestionDevice.text = "03:00";
        break;
      case 3:
        TimerTextInOuterTimerDevice.text = "03:00";
        break;
    }

    if (timerCoroutine != null) StopCoroutine(timerCoroutine);
  }

  [PunRPC] public void StartTimerRPC()
  {
    timerCoroutine = StartCoroutine(TimerCoroutine());
  }

  [PunRPC] public void Disconnect()
  {
    PhotonNetwork.LeaveRoom();
  }

  private IEnumerator TimerCoroutine()
  {
    bool isPalse = false;
    bool isBlackout = false;
    bool isNoise = false;
    bool is60secSoundPlayed = false;
    bool is120secSoundPlayed = false;
    float noiseSpan = 0;
    System.Random rand = new System.Random();
    ClockClick.Play();

    while (timer > 0)
    {
      yield return new WaitForFixedUpdate();
      if (isPalse)
      {
        switch (userIndex)
        {
          case 2:
            PalseInQuestionDevice.SetActive(true);
            break;
          case 3:
            PalseInOuterTimerDevice.SetActive(true);
            break;
        }
      }
      else
      {
        switch (userIndex)
        {
          case 2:
            PalseInQuestionDevice.SetActive(false);
            break;
          case 3:
            PalseInOuterTimerDevice.SetActive(false);
            break;
        }
      }
      timer -= Time.fixedDeltaTime;
      noiseSpan -= Time.fixedDeltaTime;
      if (timer < 0) timer = 0;
      if (userIndex == 1)
      {
        SystemTexts[3].text = $"残り {Mathf.Floor(timer / 60)}分 {Mathf.Floor(timer % 60)}秒 " + Mathf.Floor((timer - Mathf.Floor(timer)) * 1000).ToString("000");
        if (timer <= 0)
        {
          photonView.RPC(nameof(GameOverRPC), RpcTarget.AllViaServer);
        }
      }
      else if (userIndex == 2)
      {
        if (timer < 120 && !is120secSoundPlayed)
        {
          HeavyPiano.Play();
          is120secSoundPlayed = true;
        }
        if (timer < 60 && !is60secSoundPlayed)
        {
          HeavyPiano.Play();
          is60secSoundPlayed = true;
        }

        if (timer >= 60)
        {
          TimerTextInQuestionDevice.text = Mathf.Floor(timer / 60).ToString("00") + ":" + Mathf.Floor(timer % 60).ToString("00");
        }
        else
        {
          BaseTimerTextInQuestionDevice.text = "00.000";
          TimerTextInQuestionDevice.text = timer.ToString("00.000");
          TimerTextInQuestionDevice.color = Color.red;
        }
      }
      else if (userIndex == 3)
      {


        if (timer >= 60)
        {
          TimerTextInOuterTimerDevice.text = Mathf.Floor(timer / 60).ToString("00") + ":" + Mathf.Floor(timer % 60).ToString("00");
        }
        else
        {
          BaseTimerTextInOuterTimerDevice.text = "00.000";
          TimerTextInOuterTimerDevice.text = timer.ToString("00.000");
          TimerTextInOuterTimerDevice.color = Color.red;
        }
      }

      // palse
      if (userIndex != 1)
      {
        if (timer < 120)
        {
          if (rand.Next(0, (int)timer + 3) < 3)
          {
            isPalse = true;
          }
          else
          {
            isPalse = false;
          }
        }
      }

      // blackout

      // noise
      if (userIndex == 2)
      {
        if (timer < 90 && (!Noise.isPlaying || noiseSpan <= 0))
        {
          if (rand.Next(0, (int)timer + 1) < 1)
          {
            float span = rand.Next(0, 350) * 0.01f;
            Noise.time = span;
            noiseSpan = span;
            isNoise = true;
            Noise.Play();
          }
          else
          {
            isNoise = false;
          }
        }
      }
    }
    switch (userIndex)
    {
      case 2:
        PalseInQuestionDevice.SetActive(false);
        break;
      case 3:
        PalseInOuterTimerDevice.SetActive(false);
        break;
    }

    Noise.Stop();
    ClockClick.Stop();
    yield break;
  }

  [PunRPC] public void GameOverRPC()
  {
    switch (userIndex)
    {
      case 1:
        SystemTexts[3].text = "ゲームオーバーしました";
        break;
      case 2:
        GameResultObjectsInQuestionDevice[1].SetActive(true);
        break;
      case 3:
        GameResultObjectsInOuterTimerDevice[1].SetActive(true);
        break;
    }
  }

  [PunRPC] public void GameClearRPC()
  {
    StopCoroutine(timerCoroutine);
    switch (userIndex)
    {
      case 1:
        SystemTexts[3].text = "ゲームクリアしました";
        break;
      case 2:
        GameResultObjectsInQuestionDevice[0].SetActive(true);
        break;
      case 3:
        GameResultObjectsInOuterTimerDevice[0].SetActive(true);
        break;
    }
  }

  public void OnPressAnswering()
  {
    KeyboardMasterObj.SetActive(true);
  }

  public void OnPressCancel()
  {
    KeyboardMasterObj.SetActive(false);
  }

  public void OnPressAnswerSubmit()
  {
    if (AnswerFieldText.text == Questions[questionIndex].Answer)
    {
      photonView.RPC(nameof(GameClearRPC), RpcTarget.AllViaServer);
    }
    else
    {
      AnswerFieldText.text = "";
      KeyboardMasterObj.SetActive(false);
    }
  }
}
