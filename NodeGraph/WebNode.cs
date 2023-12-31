﻿using System.Collections.Generic;

public class WebNode
{
    public string Url { get; set; }
    public List<WebNode> LinkedNodes { get; set; }

    public WebNode(string url)
    {
        Url = url;
        LinkedNodes = new List<WebNode>();
    }
}