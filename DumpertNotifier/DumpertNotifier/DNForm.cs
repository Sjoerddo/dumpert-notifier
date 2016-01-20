﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Windows.Forms;
using System.Xml;

namespace DumpertNotifier
{
    public partial class DNForm : Form
    {
        private readonly Uri _rssFeed = new Uri("http://www.dumpert.nl/rss.xml.php");
        private readonly Uri _homepage = new Uri("http://www.dumpert.nl");
        private DateTime _startTime = DateTime.Now;

        private const int CpNocloseButton = 0x200;
        protected override CreateParams CreateParams
        {
            get
            {
                var myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CpNocloseButton;
                return myCp;
            }
        }

        public DNForm()
        {
            InitializeComponent();
            cbRefresh.SelectedIndex = 0;
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void _notifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            Process.Start((GetFirstItemFromFeed(_rssFeed) ?? _homepage).ToString());
        }

        public static SyndicationFeed GetFeed(Uri uri)
        {
            Uri uriResult;
            if (!Uri.TryCreate(uri.AbsoluteUri, UriKind.Absolute, out uriResult) ||
                uriResult.Scheme != Uri.UriSchemeHttp)
            {
                return null;
            }

            try
            {
                using (var reader = XmlReader.Create(uri.AbsoluteUri))
                {
                    return SyndicationFeed.Load(reader);
                }
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show(string.Format(@"Unable to get data from '{0}'", uri.AbsoluteUri));
                return null;
            }
        }

        /// <summary>
        ///     Parses the RSS feed located at the specified Uri and returns the first available Uri or null if no valid Uri are
        ///     found
        /// </summary>
        private static Uri GetFirstItemFromFeed(Uri feedUri)
        {
            return GetFeed(feedUri).Items
                .SelectMany(item => item.Links) // take all urls from all items
                .Select(link => link.Uri) // convert the SyndicationLink's to Uri's
                .FirstOrDefault(); // return the first or null
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            var lastItem = GetItem();
            var lastUpdated = lastItem.PublishDate.DateTime;
            if (lastUpdated <= _startTime) return;

            _startTime = lastUpdated;
            _notifyIcon.ShowBalloonTip(5000, "Nieuw filmpje!",
                string.Format("{0}\n{1}\n{2}", lastItem.Title.Text, lastItem.Summary.Text,
                    lastUpdated.ToShortTimeString()), ToolTipIcon.Info);
        }

        public SyndicationItem GetItem()
        {
            return GetFeed(_rssFeed).Items
                .OrderByDescending(date => date.PublishDate)
                .FirstOrDefault();
        }

        private void quitMenuItem_Click(object sender, EventArgs e)
        {
            Close();      
        }

        private void settingsMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Hide();
            _timer.Interval = Convert.ToInt32(cbRefresh.SelectedItem)*1000;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Hide();
        }
    }
}