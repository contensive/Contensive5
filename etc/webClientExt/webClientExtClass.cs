﻿
using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Net;
using System.Collections.Generic;

public class webClientExt : WebClient
{
    private System.Net.CookieContainer cookieContainer;
    private string userAgent;
    private int timeout;
    private string privateUsername;
    private string privatePassword;
    private int cookieCnt = 0;
    //
    public void addCookie(string cookieName, string cookieValue)
    {
        cookieContainer.Add( new Cookie(cookieName, cookieValue,"/"));
        cookieCnt+=1;
    }
    //
    public string username
    {
        get
        {
            return privateUsername;
        }
        set
        {
            privateUsername = value;
        }
    }
    //
    public string password
    {
        get
        {
            return privatePassword;
        }
        set
        {
            privatePassword = value;
        }
    }

    public System.Net.CookieContainer CookieContainer
    {
        get { return cookieContainer; }
        set { cookieContainer = value; }
    }

    public string UserAgent
    {
        get { return userAgent; }
        set { userAgent = value; }
    }

    public int Timeout
    {
        get { return timeout; }
        set { timeout = value; }
    }

    public webClientExt()
    {
        timeout = -1;
        userAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727)";
        cookieContainer = new CookieContainer();
        //cookieContainer.Add(new Cookie("example", "example_value"));
    }

    protected override WebRequest GetWebRequest(Uri address)
    {
        WebRequest request = base.GetWebRequest(address);
        RefreshUserAgent();

        if (request.GetType() == typeof(HttpWebRequest))
        {
            ((HttpWebRequest)request).CookieContainer = cookieContainer;
            ((HttpWebRequest)request).UserAgent = userAgent;
            ((HttpWebRequest)request).Timeout = timeout;
            if (privateUsername != "")
            {
                NetworkCredential myCred = new NetworkCredential(privatePassword, privatePassword);
                ((HttpWebRequest)request).Credentials = myCred;
            }
        }

        return request;
    }

    private void RefreshUserAgent()
    {
        List<string> UserAgents = new List<string>();
        UserAgents.Add("Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; .NET CLR 2.0.50727)");
        UserAgents.Add("Mozilla/4.0 (compatible; MSIE 8.0; AOL 9.5; AOLBuild 4337.43; Windows NT 6.0; Trident/4.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.5.21022; .NET CLR 3.5.30729; .NET CLR 3.0.30618)");
        UserAgents.Add("Mozilla/4.0 (compatible; MSIE 7.0; AOL 9.5; AOLBuild 4337.34; Windows NT 6.0; WOW64; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; .NET CLR 3.5.30729; .NET CLR 3.0.30618)");
        UserAgents.Add("Mozilla/5.0 (X11; U; Linux i686; pl-PL; rv:1.9.0.2) Gecko/20121223 Ubuntu/9.25 (jaunty) Firefox/3.8");
        UserAgents.Add("Mozilla/5.0 (Windows; U; Windows NT 5.1; ja; rv:1.9.2a1pre) Gecko/20090402 Firefox/3.6a1pre (.NET CLR 3.5.30729)");
        UserAgents.Add("Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1b4) Gecko/20090423 Firefox/3.5b4 GTB5 (.NET CLR 3.5.30729)");
        UserAgents.Add("Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; Avant Browser; .NET CLR 2.0.50727; MAXTHON 2.0)");
        UserAgents.Add("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; Media Center PC 6.0; InfoPath.2; MS-RTC LM 8)");
        UserAgents.Add("Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.0; WOW64; Trident/4.0; SLCC1; .NET CLR 2.0.50727; Media Center PC 5.0; InfoPath.2; .NET CLR 3.5.21022; .NET CLR 3.5.30729; .NET CLR 3.0.30618)");
        UserAgents.Add("Mozilla/4.0 (compatible; MSIE 7.0b; Windows NT 6.0)");
        UserAgents.Add("Mozilla/4.0 (compatible; MSIE 7.0b; Windows NT 5.1; Media Center PC 3.0; .NET CLR 1.0.3705; .NET CLR 1.1.4322; .NET CLR 2.0.50727; InfoPath.1)");
        UserAgents.Add("Opera/9.70 (Linux i686 ; U; zh-cn) Presto/2.2.0");
        UserAgents.Add("Opera 9.7 (Windows NT 5.2; U; en)");
        UserAgents.Add("Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.8.1.8pre) Gecko/20070928 Firefox/2.0.0.7 Navigator/9.0RC1");
        UserAgents.Add("Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.8.1.7pre) Gecko/20070815 Firefox/2.0.0.6 Navigator/9.0b3");
        UserAgents.Add("Mozilla/5.0 (Windows; U; Windows NT 5.1; en) AppleWebKit/526.9 (KHTML, like Gecko) Version/4.0dp1 Safari/526.8");
        UserAgents.Add("Mozilla/5.0 (Windows; U; Windows NT 6.0; ru-RU) AppleWebKit/528.16 (KHTML, like Gecko) Version/4.0 Safari/528.16");
        UserAgents.Add("Opera/9.64 (X11; Linux x86_64; U; en) Presto/2.1.1");

        Random r = new Random();
        this.UserAgent = UserAgents[r.Next(0, UserAgents.Count)];

        UserAgents = null;
    }
}