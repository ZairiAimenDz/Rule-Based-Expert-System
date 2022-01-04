using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SystemExperts.Models;
using System.Collections.ObjectModel;
using System.Collections;
using Microsoft.Win32;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SystemExperts
{
    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Rule> Rules = new ObservableCollection<Rule>();
        public ObservableCollection<string> FactsInit = new ObservableCollection<string>();
        public ObservableCollection<string> Facts = new ObservableCollection<string>();
        public ObservableCollection<string> Progress = new ObservableCollection<string>();
        
        // TODO : Show initial Facts
        public MainPage()
        {
            this.InitializeComponent();
        }


        private void DeleteRule(object sender, RoutedEventArgs e)
        {
            var rule = RulesList.SelectedItem as Rule;
            if (rule != null)
                Rules.Remove(rule);
            RulesList.ItemsSource = Rules;
        }

        private void DeleteFact(object sender, RoutedEventArgs e)
        {
            var fact = FactsList.SelectedItem as string;
            if (!string.IsNullOrEmpty(fact))
            {
                Facts.Remove(fact);
                FactsInit.Remove(fact);
            }
            FactsList.ItemsSource = Facts;
        }

        private void AddRule(object sender, RoutedEventArgs e)
        {
            var rule = Rule.StringToRule(InputTextBox.Text, ResultsTextBox.Text);
            rule.Number = Rules.Count() + 2;
            Rules.Add(rule);
            RulesList.ItemsSource = Rules;
        }

        private void AddFact(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(FactTextBox.Text))
            {
                FactTextBox.Text.Split(',').ToList().ForEach(fact => Facts.Add(fact));
                FactTextBox.Text.Split(',').ToList().ForEach(fact => FactsInit.Add(fact));
            }
            FactsList.ItemsSource = Facts;
        }

        private void ForwardChainning(object sender, RoutedEventArgs e)
        {
            Progress.Clear();
            Stack<int> stck = new Stack<int>();
            stck.Clear();
            foreach (var r in Rules)
            {
                if (!r.Outputs.All(a => Facts.Contains(a)))
                    if (r.Inputs.All(a => Facts.Contains(a)))
                    {
                        r.Outputs.ForEach(el => Facts.Add(el));
                        Progress.Add("R" + (Rules.IndexOf(r) + 1) + "\t Est Appliquer \t- Fact Table : "
                            + String.Join(',', Facts) + " \t- Pile : " + String.Join(',', stck));
                    }
                    else
                    {
                        stck.Push(Rules.IndexOf(r));
                        Progress.Add("R" + (Rules.IndexOf(r) + 1) + "\t Pas Appliquer \t- Fact Table : "
                            + String.Join(',', Facts) + " \t- Pile : " + String.Join(',', stck));

                    }
                else
                    Progress.Add("R" + (Rules.IndexOf(r) + 1) + "\t est inutule  \t- Fact Table : " 
                        + String.Join(',', Facts));


                ChainList.ItemsSource = Progress;
            }

            Progress.Add("Retour Dans La Pile" + "\t- Fact Table : " + String.Join(',', Facts));
            ChainList.ItemsSource = Progress;
            while (stck.Count > 0)
            {
                var rule = Rules.ElementAt(stck.Pop());
                if (rule.Inputs.All(b => Facts.Contains(b)))
                {
                    rule.Outputs.ForEach(el => Facts.Add(el));
                    Progress.Add("R" + (Rules.IndexOf(rule) + 1) + "\t Est Appliquer \t- Fact Table : "
                        + String.Join(',', Facts) + " \t- Pile : " + String.Join(',', stck));
                }
                else
                {
                    Progress.Add("On Peut Appliquer R" + (Rules.IndexOf(rule) + 1) + " On Arrete");
                    break;
                }


                ChainList.ItemsSource = Progress;
            }
        }

        private void BackwardChainning(object sender, RoutedEventArgs e)
        {
            Progress.Clear();
            FinalChain.Text = "";
            var wantedres = WantedRes.Text;
            var chainres = ChainBackward(wantedres);
            // Progress Show The Chaining ets
            Progress.Add(chainres.ToString());
            ChainList.ItemsSource = Progress;
        }

        private bool ChainBackward(string wantedres)
        {
            if (wantedres == null)
                return false;
            else
            {
                if (Facts.Contains(wantedres))
                    return true;
                // Backward Chainning
                var ER = Rules.Where(r => r.Outputs.Contains(wantedres)).ToList();
                if (ER.Any())
                {
                    bool verify;
                    //
                    Progress.Add("Proving " + wantedres + " :");
                    //
                    while (ER.Count() > 0)
                    {
                        verify = true;
                        var r = ER.First();
                        Progress.Add("R" + (Rules.IndexOf(r) + 1) + " To Prove " + wantedres);
                        foreach (var item in r.Inputs)
                        {
                            verify = verify && ChainBackward(item);
                            if (!verify)
                            {
                                Progress.Add("R" + (Rules.IndexOf(r) + 1) + " Can't Prove " + wantedres);
                                break;
                            }
                            // 
                        }
                        if (verify)
                        {
                            Facts.Add(wantedres);
                            Progress.Add(wantedres + " is Proven by R" + (Rules.IndexOf(r)+1));
                            FinalChain.Text = !string.IsNullOrEmpty(FinalChain.Text) ? FinalChain.Text + " -> R" + (Rules.IndexOf(r) + 1) : "R" + (Rules.IndexOf(r) + 1);
                            return true;
                        }
                        else
                            ER.Remove(r);
                    }
                    return false;
                }
                else
                {
                    //
                    Progress.Add("Can't Prove " + wantedres);
                    //
                    return false;
                }
            }
        }

        private async void LoadRulesFromFile(object sender, RoutedEventArgs e)
        {
            try
            {
                Rules.Clear();
                Facts.Clear();
                Progress.Clear();
                FactsList.ItemsSource = Facts;
                ChainList.ItemsSource = Progress;

                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.List;
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".txt");
                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    var readstream = stream.AsStreamForRead();
                    StreamReader reader = new StreamReader(readstream);
                    var rules = reader.ReadToEnd().Split('\n',StringSplitOptions.RemoveEmptyEntries);
                    int currnum = 1;
                    foreach (var rule in rules)
                    {
                        var elts = rule.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                        var r = Rule.StringToRule(elts[2], elts[4]);
                        r.Number = currnum;
                        Rules.Add(r);
                        currnum++;
                    }
                    RulesList.ItemsSource = Rules;
                }
            }
            catch
            {
                foreach (var line in await Windows.Storage.FileIO.ReadLinesAsync(await StorageFile.GetFileFromPathAsync("B:\\rules.txt")))
                {
                    Rules.Add(Rule.StringToRule(line.Remove(0, 2).Trim().Split("alors").First().Trim()
                        , line.Remove(0, 2).Trim().Split("alors").Last().Trim()));
                }
            }
        }
    }
}
