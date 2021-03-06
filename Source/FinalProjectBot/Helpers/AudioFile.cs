﻿using System;
using System.Collections.Generic;
using System.Text;


namespace FinalProjectBot.Helpers
{
    public class AudioFile
    {
        //Collect Data needed for audio to play
        private string m_FileName;
        private string m_Title;
        private bool m_IsNetwork;
        private bool m_IsDownloaded;

        public AudioFile()
        {
            m_FileName = "";
            m_Title = "";
            m_IsNetwork = true; // True by default
            m_IsDownloaded = false;
        }

        public override string ToString()
        {
            return m_Title;
        }

        public string FileName
        {
            get { return m_FileName; }
            set { m_FileName = value; }
        }

        public string Title
        {
            get { return m_Title; }
            set { m_Title = value; }
        }

        public bool IsNetwork
        {
            get { return m_IsNetwork; }
            set { m_IsNetwork = value; }
        }

        public bool IsDownloaded
        {
            get { return m_IsDownloaded; }
            set { m_IsDownloaded = value; }
        }

    }
}
