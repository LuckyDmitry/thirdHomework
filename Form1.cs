using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace ThirdHomework
{
    public partial class Form1 : Form
    {
        private string text;
        private Dictionary<int, String> sentences;
        private string[] insertQeuries;
        private Index index;
        private Npgsql.NpgsqlConnection connection;
        private Operation operation;
        public Form1()
        {
            InitializeComponent();
            Init();
        }

        ~Form1()
        {
            connection.Close();
        }

        private void Init()
        {
            string connectionParameters = "Server=localhost;Port=5432;Username=postgres;Password=123;Database=third;";
            connection = new Npgsql.NpgsqlConnection(connectionParameters);
            sentences = new Dictionary<int, string>();
            insertQeuries = new string[0];
            connection.Open();
            PopulateStringValues();
            index = Index.noIndex;
        }

        public void MakeQuery(string query)
        {
            NpgsqlCommand t_CommandIfExist = new NpgsqlCommand(query);
            t_CommandIfExist.Connection = connection;
            NpgsqlDataReader reader = t_CommandIfExist.ExecuteReader();
            reader.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            text = textBox.Text;
        }

        private void InsertButtonClicked(object sender, EventArgs e)
        {
            operation = Operation.insert;
            int amount = GetValueFromTextField();
            if (amount > 0) {
                GenerateInsertQueries(amount);
                TimeSpan time = TimeMeasure(GenerateEntries, amount);
                ShowMessage($"Generation finished with {time.TotalSeconds}");
                listBox1.Items.Add($"Operation {operation} with {index} took {time.TotalSeconds} for {amount}");
            } else
            {
                ShowMessage("Enter number above zero");
            }
        }

        private int GetValueFromTextField()
        {
            if (text != null && text.Length > 0)
            {
                if (int.TryParse(text, out int val))
                {
                    return val;
                }
                else
                {
                    ShowMessage("Enter correct number");
                }
            }
            else
            {
                ShowMessage("Enter any number!");
            }
            return -1;
        }
        
        private TimeSpan TimeMeasure<T>(Action<T> func, T args) 
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            func(args);
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
        private void GenerateEntries(int amount)
        {
            Random random = new Random();
            for (int i = 0; i < amount; ++i)
            {
                try
                {
                    string query = insertQeuries[random.Next(0, insertQeuries.Length - 1)];
                    MakeQuery(query);
                }
                catch (Exception ex)
                {
                    ShowMessage(ex.Message);
                }
            }
        }

        private void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }

        private void SelectButtonClicked(object sender, EventArgs e)
        {
            int amount = GetValueFromTextField();
            var totalTime = TimeMeasure(selectMethod, amount);
            
            listBox1.Items.Add($"Operation {operation} with {index} took {totalTime.TotalSeconds} for {amount}");
            ShowMessage($"Generation finished with {totalTime.TotalSeconds}");
        }

        private void selectMethod(int amount)
        {
           
            operation = Operation.select;
            TimeSpan totalTime = TimeSpan.FromMilliseconds(0);
            Random random = new Random();

            for (int i = 0; i < amount; ++i)
            {
                    MakeQuery($"select * from public.{Table.tableName} where {Table.firstColumn} = {random.Next(0, 40000)}");
            }
        }

        private void GenerateInsertQueries(int amount)
        {
            Random random = new Random();
            insertQeuries = new string[amount];
            StringBuilder builder = new StringBuilder();
            string[] result = new string[amount];
            for (int i = 0; i < amount; ++i)
            {
                builder.Clear();
                builder.Append(random.Next(1, 3));
                builder.Append(",");
                builder.Append($"'{sentences[random.Next(1, sentences.Keys.Count - 1)]}'");
                string s = builder.ToString();

                insertQeuries[i] = $"insert into {Table.tableName} ({Table.secondColumn}, {Table.thirdColumn}) values ({s});";
            }
        }

        private void PopulateStringValues()
        {
            int wordsAmount = 100;
            int sentenceAmount = 40;
            var random = new Random();

            string[] words = new string[wordsAmount];

            while (wordsAmount > 0)
            {
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Clear();
                for (int i = 0; i < random.Next(4, 8); i++)
                {
                    stringBuilder.Append(chars[random.Next(chars.Length)]);
                }
                words[wordsAmount - 1] = stringBuilder.ToString();
                wordsAmount--;
            }

            for(int i = 0;i < sentenceAmount;++i)
            {
                sentences[i] = $"{words[random.Next(0, 99)]} {words[random.Next(0, 99)]} {words[random.Next(0, 99)]}";
            }
        }

        private void DropIndex()
        {
            MakeQuery($"drop index if exists {Table.indexName}");
        }

        private void RadioButtonCheckedChanged(object sender, EventArgs e)
        {
            DropIndex();
            if (radioButton2.Checked)
            {
                index = Index.btree;
            } else if (radioButton3.Checked)
            {
                index = Index.hash;
            } else if (radioButton4.Checked)
            {
                index = Index.gin;
            } else
            {
                index = Index.noIndex;
            }
            CreateIndex(index);
        }

        private void SelectByConditionButtonClicked(object sender, EventArgs e)
        {
            operation = Operation.selectByCondition;
            int amount = GetValueFromTextField();
            var totalTime = TimeMeasure(conditionSelect, amount);
           
            listBox1.Items.Add($"Operation {operation} with {index} took {totalTime.TotalSeconds} for {amount}");
            ShowMessage($"Generation finished with {totalTime.TotalSeconds}");
        }

        private void conditionSelect(int amount)
        {
            Random random = new Random();
            for (int i = 0; i < amount; ++i)
            {
                MakeQuery($"select * from public.{Table.tableName} where {Table.firstColumn} < {random.Next(1, 100000)} AND {Table.firstColumn} > {random.Next(1, 50000)};");            
            }
        }

        private void CreateIndex(Index index)
        {

            string column = Table.firstColumn;
            string idx = "";
            switch (index) 
            {
                case Index.btree:
                    {
                        idx = "btree";
                        break;
                    }
                case Index.hash:
                    {
                        idx = "hash";
                        break;
                    }
                case Index.gin:
                    {
                        idx = "gin";
                        column = Table.thirdColumn;
                        MakeQuery($"create index {Table.indexName} on public.{Table.tableName} using {idx}({column} gin_trgm_ops)");
                        
                        break;
                    }
            }

            if (idx.Length > 0 && idx != "gin")
                MakeQuery($"create index {Table.indexName} on public.{Table.tableName} using {idx}({column})");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            int amount = GetValueFromTextField();
            var totalTime = TimeMeasure(thirdColSelect, amount);
            
            listBox1.Items.Add($"Operation {operation} with {index} took {totalTime.TotalSeconds} for {amount}");
            ShowMessage($"Generation finished with {totalTime.TotalSeconds}");
        }

        private void thirdColSelect(int amount)
        {
            TimeSpan totalTime = TimeSpan.FromMilliseconds(0.0);
            Random random = new Random();
            for (int i = 0; i < amount; ++i)
            {
                var q = $"select * from public.{Table.tableName} where {Table.thirdColumn} like '{sentences[random.Next(1, sentences.Count)]}';";
                MakeQuery(q);
            }
        }
    }

    public static class Table
    {
        public const string
            tableName = "test_table",
            firstColumn = "idx2",
            secondColumn = "second_field",
            indexName = "index_name",
            thirdColumn = "third_field";
    }

    public enum Index
    {
        noIndex,
        hash, 
        btree,
        gin
    }

    public enum Operation
    {
        select, 
        insert,
        selectByCondition
    }
}
