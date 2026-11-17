using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitJson;
using UnityEngine;


//메일 의 로직에 관한 정의입니다.
public class MailService : IService
{
    private readonly List<MailData> mails = new List<MailData>();

    private MailBackEnd mailBackEnd;    
    private RewardService rewardService;
    private bool isUpdateingMail = false;
    
    public void Initialize()
    {
        mailBackEnd = GlobalManagers.BackEnd.Mail;
        rewardService = GlobalManagers.Service.GetService<RewardService>();
        GlobalManagers.Event?.AddEvent<OnNewPostCreatedEventData>(OnNewPostCreatedEventHandler);
    }

    public void Release()
    {
        GlobalManagers.Event?.RemoveEvent<OnNewPostCreatedEventData>(OnNewPostCreatedEventHandler);    
    }

    /// <summary>
    /// 새로운 우편을 받을 시 현재 캐싱된 우편을 업데이트 합니다.
    /// </summary>
    /// <param name="e"></param>
    private void OnNewPostCreatedEventHandler(OnNewPostCreatedEventData e)
        => UpdateMailListAsync().Forget();

    
    
    private async UniTask UpdateMailListAsync()
    {
        await UniTask.Delay(GlobalDefines.GameConstants.MAIL_REQUEST_DELAY);
        UpdateMailListAsync(GlobalDefines.GameConstants.MAIL_REQUEST_COUNT).Forget();
    }


    /// <summary>
    /// 매일의 데이터를 파싱해서 가져오고 매일을 업데이트 합니다.
    /// </summary>
    /// <returns></returns>
    public async UniTask<bool> UpdateMailListAsync(int count)
    {
        if (isUpdateingMail) return false;

        isUpdateingMail = true;

        try
        {
            await UniTask.SwitchToMainThread();
            JsonData jsonData = await mailBackEnd.GetMailListAsync(count);

            if (jsonData == null)
            {
                Logger.LogError("매일을 가져올 수 없습니다.");
                return false;
            }

            Logger.Log($"우편 데이터 수신 완료. 개수: {jsonData} {jsonData.Count}");
            var newMails = new List<MailData>();

            for (int i = 0; i < jsonData.Count; i++)
            {
                MailData mail = new MailData();
                mail.title = jsonData[i]["title"].ToString();
                mail.content = jsonData[i]["content"].ToString();
                mail.inDate = jsonData[i]["inDate"].ToString();
                mail.reservationDate = DateTime.Parse(jsonData[i]["reservationDate"].ToString());
                mail.expirationDate = DateTime.Parse(jsonData[i]["expirationDate"].ToString());
                mail.sentDate = DateTime.Parse(jsonData[i]["sentDate"].ToString());

                mail.mailRewardItemDatas = new List<MailCurrencyData>();

                if (jsonData[i]["items"].Count > 0)
                {
                    for (int itemNum = 0; itemNum < jsonData[i]["items"].Count; itemNum++)
                    {

                        MailCurrencyData mailCurrency = new MailCurrencyData();
                        mailCurrency.amount = int.Parse(jsonData[i]["items"][itemNum]["itemCount"].ToString());
                        mailCurrency.chartFileName = jsonData[i]["items"][itemNum]["item"]["chartFileName"].ToString();
                        mailCurrency.itemID = jsonData[i]["items"][itemNum]["item"]["Key"].ToString();
                        mailCurrency.currencyName = jsonData[i]["items"][itemNum]["item"]["CurrencyName"].ToString();
                        mailCurrency.currencyInfo = jsonData[i]["items"][itemNum]["item"]["CurrencyInfo"].ToString();

                        mail.mailRewardItemDatas.Add(mailCurrency);
                    }
                }

                newMails.Add(mail);
            }

            mails.Clear();
            mails.AddRange(newMails);

            GlobalManagers.Event.NotifyEvent<OnPostMailLoadedEventData>(new OnPostMailLoadedEventData
            {
                MailCount = mails.Count,
            });
        }
        catch (Exception e)
        {
            Logger.LogError($"{e.Message} 에러가 발생했습니다.");
            return false;
        }
        finally
        {
            isUpdateingMail = false;
        }

        return true;
    }


    /// <summary>
    /// 지정된 개수 만큼 매일 데이터를 가져옵니다.
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public List<MailData> GetMails(int count)
    {
        List<MailData> res =  new List<MailData>(mails);
        return res;
    }
    
    
    /// <summary>
    /// inDate에 해당하는 메일이 있는지 확인 합니다.
    /// </summary>
    /// <param name="inDate"></param>
    /// <returns></returns>
    public bool IsExistMail(string inDate)
    {
        foreach(MailData  mail in mails)
        {
            if (mail.inDate == inDate)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 매일을 수령합니다
    /// </summary>
    /// <param name="inDate">매일의 inDate 가 필요합니다</param>
    /// <returns></returns>
    public async UniTask<bool> ReceiveMailAsync(string inDate)
    {
        if (!IsExistMail(inDate))
        {
            Logger.LogError($"{inDate}에 해당하는 메일이 없습니다");
            return false;
        }

        bool res = await mailBackEnd.RecieveMailAsync(inDate);
        
        if (res)
        {
            int fIndex = mails.FindIndex(x => x.inDate == inDate);
            
            List<MailCurrencyData> mailCurrencyDatas = mails[fIndex].mailRewardItemDatas;

            foreach (var mailCurrencyData in mailCurrencyDatas)
            {
                //나중에 리워드를 인터페이스나 추상클래스로 묶어서 대응하는게 좋을듯 하다
                int amount = mailCurrencyData.amount;
                string rewardID = mailCurrencyData.itemID;
                RewardData rewardData = new RewardData(ERewardType.Currency, rewardID, amount);
                rewardService.GiveReward(rewardData);
            }

            mails.RemoveAt(fIndex);
        }
        else
        {
            GlobalManagers.Event.NotifyEvent<OnBackEndErrorEventData>(new OnBackEndErrorEventData()
            {
                ErrorType = EBackEndErrorType.RequestFailed
            });
        }

        return res;
    }   


}
