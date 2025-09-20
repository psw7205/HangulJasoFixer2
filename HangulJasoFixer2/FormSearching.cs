using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HangulJasoFixer2
{
    public partial class FormSearching : Form
    {
        private SearchArguments searchArguments;
        private CancellationTokenSource _cancellationTokenSource;

        public FormSearching()
        {
            InitializeComponent();
        }

        public void SetCriteria(SearchCriteria searchCriteria)
        {
            searchArguments = new SearchArguments(searchCriteria, labelCurrentFile);
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            buttonCancel.Enabled = false;
        }

        private void FormSearching_Shown(object sender, EventArgs e)
        {
            backgroundWorkerSearch.RunWorkerAsync(searchArguments);
        }

        private void BackgroundWorkerSeaching_DoWork(object sender, DoWorkEventArgs e)
        {
            var searchArgs = e.Argument as SearchArguments;
            searchArgs.ClearRows();

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                SearchRecursivelyAsync(searchArgs, _cancellationTokenSource.Token).Wait();
            }
            catch (OperationCanceledException)
            {
                e.Cancel = true;
            }
        }

        private async Task SearchRecursivelyAsync(SearchArguments args, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                var directoryInfo = new DirectoryInfo(args.RootPath);

                foreach (var fi in directoryInfo.GetFiles())
                {
                    token.ThrowIfCancellationRequested();
                    args.SetCurrentFileLable(fi.Name);
                    if (!fi.Name.IsNormalized())
                    {
                        string fixedFullName = Path.Combine(fi.DirectoryName, fi.Name.Normalize());
                        args.AddRow(fi.FullName, fixedFullName, "파일");
                    }
                }

                var subDirectories = directoryInfo.GetDirectories();
                var tasks = new List<Task>();

                foreach (var di in subDirectories)
                {
                    token.ThrowIfCancellationRequested();

                    if (args.IsIncludeSubDirectory)
                    {
                        tasks.Add(SearchRecursivelyAsync(args.Clone(di.FullName), token));
                    }

                    args.SetCurrentFileLable(di.Name);
                    if (args.IsIncludeDirectory && !Path.GetFileName(di.FullName).IsNormalized())
                    {
                        string fixedFullName = Path.Combine(Path.GetDirectoryName(di.FullName), Path.GetFileName(di.FullName).Normalize());
                        args.AddRow(di.FullName, fixedFullName, "폴더");
                    }
                }

                await Task.WhenAll(tasks);
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void BackgroundWorkerSeaching_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBarSearching.Value = e.ProgressPercentage;
        }

        private void BackgroundWorkerSeaching_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Close();
        }

        private void FormSearching_FormClosing(object sender, FormClosingEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
