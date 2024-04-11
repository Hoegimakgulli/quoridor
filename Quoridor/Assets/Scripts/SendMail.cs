using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class SendMail : MonoBehaviour
{
    string LoadLog(string filePath)
    {
        string log = "";
        try
        {
            log = System.IO.File.ReadAllText(filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
        return log;
    }
    public void OpenMailWithAttachment()
    {
        string fileName = GetComponentInChildren<Text>().text;
        string email = "ydh910.b@gmail.com";
        string subject = fileName;
        string body = $"대략적인 상황 설명을 해주세요.\n\n 추가로 이미지나 영상 파일을 첨부해주시면 더욱 빠른 해결이 가능합니다.\n\n감사합니다!\n\n\n\n---------------------------------\n\n\n\n{LoadLog(Application.persistentDataPath + "/logs/" + fileName + ".log")}";
        // string attachmentPath = Application.persistentDataPath + "/logs/" + fileName + ".log"; // 파일 경로

        string uri = "mailto:" + email + "?subject=" + subject + "&body=" + body;
        Application.OpenURL(uri);


        // string fileName = GetComponentInChildren<Text>().text;
        // MailMessage mail = new MailMessage();

        // mail.From = new MailAddress("ydh910.b@gmail.com");
        // mail.To.Add("ydh910.b@gmail.com");
        // mail.Subject = fileName;
        // mail.Body = $"대략적인 상황 설명을 해주세요.\n\n 추가로 이미지나 영상 파일을 첨부해주시면 더욱 빠른 해결이 가능합니다.\n\n감사합니다!";
        // Attachment attachment = new Attachment(Application.persistentDataPath + "/logs/" + fileName + ".log");
        // mail.Attachments.Add(attachment);
        // SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
        // smtpServer.Port = 587;
        // smtpServer.Credentials = new System.Net.NetworkCredential("ydh910.b@gmail.com", "bgmb lmnb brta jyky") as ICredentialsByHost;
        // smtpServer.EnableSsl = true;
        // ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };

        // smtpServer.Send(mail);

        transform.parent.parent.GetComponent<SendMail>().OpenPanel();
    }
    bool isOpened = false;
    public void OpenPanel()
    {
        GameObject panel = transform.GetChild(1).gameObject;
        if (isOpened)
        {
            panel.SetActive(false);
            isOpened = false;
            return;
        }
        panel.SetActive(true);
        isOpened = true;
        string filePath = Application.persistentDataPath + "/logs/";

        string[] files = System.IO.Directory.GetFiles(filePath);

        List<string> fileNames = new List<string>();

        foreach (string file in files)
        {
            fileNames.Add(file.Split('/').Last().Split('.')[0]);
        }
        fileNames.Reverse();

        for (int i = 0; i < fileNames.Count; i++)
        {
            if (i >= 5) break;
            panel.transform.GetChild(i).GetChild(0).GetComponent<Text>().text = fileNames[i];
        }
    }
    // 파일 이름을 숫자 부분을 기준으로 정렬하는 비교 함수
    private int GetDateInt(string a)
    {
        string[] dates = a.Split("-_");

        return int.Parse($"{dates[1]}{dates[2]}{dates[3]}{dates[4]}{dates[5]}{dates[6]}");
    }
}
