using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NameGnome
{
    public partial class MainForm : Form
    {
        public static List<string> Adjectives { get; private set; }
        public static List<string> Adverbs { get; private set; }
        public static List<string> Nouns { get; private set; }
        public static List<string> Senses { get; private set; }
        public static List<string> Verbs { get; private set; }
        public static List<string> Consonants { get; private set; }
        public static List<string> Vowels { get; private set; }
        public static Random Random { get; private set; }
        private Node root;

        public MainForm()
        {
            InitializeComponent();
            Random = new Random();

            Consonants = new List<string>
            {
                "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "n", "p", "q", "r", "s", "t", "v", "w", "x", "y", "z"
            };

            Vowels = new List<string>
            {
                "a", "e", "i", "o", "u"
            };

            root = Node.Parse(txtPattern.Text);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Adjectives = getWords("NameGnome.dict.index.adj");
            Adverbs = getWords("NameGnome.dict.index.adv");
            Nouns = getWords("NameGnome.dict.index.noun");
            Senses = getWords("NameGnome.dict.index.sense");
            Verbs = getWords("NameGnome.dict.index.verb");
        }

        private List<string> getWords(string resource)
        {
            List<string> ret = new List<string>();
            var assembly = Assembly.GetExecutingAssembly();

            Regex badChars = new Regex("[\\d_]", RegexOptions.Compiled);

            using (var stream = assembly.GetManifestResourceStream(resource))
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line.StartsWith(" "))
                    {
                        continue;
                    }

                    var word = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)[0];

                    if (!badChars.IsMatch(word))
                    {
                        ret.Add(word);
                    }
                }
            }

            return ret;
        }

        private void txtPattern_TextChanged(object sender, EventArgs e)
        {
            // Attempt to parse pattern
            root = Node.Parse(txtPattern.Text);
            if (root == null)
            {
                txtPattern.ForeColor = Color.DarkRed;
            }
            else
            {
                txtPattern.ForeColor = Color.Black;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (chkAuto.Checked && root != null)
            {
                Generate();
            }
        }

        private void chkAuto_CheckedChanged(object sender, EventArgs e)
        {
            timer1.Enabled = chkAuto.Checked;
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            Generate();
        }

        private void Generate()
        {
            if (root != null)
            {
                var name = root.Generate();
                lstHistory.Items.Insert(0, name);
                lblGenerated.Text = name;
            }
        }

        private void lstHistory_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (lstHistory.SelectedItem != null)
            {
                Clipboard.SetText(lstHistory.SelectedItem.ToString());
            }
        }
    }

    class Node
    {
        private List<Node> children;
        private Type type;
        private bool capitalize;
        private string value;

        public Node()
        {
            children = new List<Node>();
            type = Type.Series;
            capitalize = false;
            value = "";
        }

        public static Node Parse(string pattern)
        {
            Node ret = new Node();

            string current = "";

            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i] == '$')
                {
                    if (current != "")
                    {
                        Node literal = new Node();
                        literal.value = current;
                        literal.type = Type.Literal;
                        ret.children.Add(literal);
                    }

                    current = "";

                    Node child = new Node();
                    i++;
                    if (i < pattern.Length)
                    {
                        switch (pattern[i])
                        {
                            case 'd':
                                child.type = Type.Adverb;
                                break;
                            case 'D':
                                child.type = Type.Adverb;
                                child.capitalize = true;
                                break;
                            case 'a':
                                child.type = Type.Adjective;
                                break;
                            case 'A':
                                child.type = Type.Adjective;
                                child.capitalize = true;
                                break;
                            case 'n':
                                child.type = Type.Noun;
                                break;
                            case 'N':
                                child.type = Type.Noun;
                                child.capitalize = true;
                                break;
                            case 's':
                                child.type = Type.Sense;
                                break;
                            case 'S':
                                child.type = Type.Sense;
                                child.capitalize = true;
                                break;
                            case 'r':
                                child.type = Type.Verb;
                                break;
                            case 'R':
                                child.type = Type.Verb;
                                child.capitalize = true;
                                break;
                            case 'c':
                                child.type = Type.Consonant;
                                break;
                            case 'C':
                                child.type = Type.Consonant;
                                child.capitalize = true;
                                break;
                            case 'v':
                                child.type = Type.Vowel;
                                break;
                            case 'V':
                                child.type = Type.Vowel;
                                child.capitalize = true;
                                break;
                            default:
                                return null;
                        }

                        ret.children.Add(child);
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (pattern[i] == '\\')
                {
                    i++;
                    if (i < pattern.Length)
                    {
                        current += pattern[i];
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (pattern[i] == '(')
                {
                    if (current != "")
                    {
                        Node literal = new Node();
                        literal.value = current;
                        literal.type = Type.Literal;
                        ret.children.Add(literal);
                    }

                    current = "";

                    int depth = 1;
                    int end = -1;
                    for (int j = i + 1; j < pattern.Length; j++)
                    {
                        if (pattern[j] == ')' && pattern[j - 1] != '\\')
                        {
                            depth--;
                        }
                        else if (pattern[j] == '(' && pattern[j - 1] != '\\')
                        {
                            depth++;
                        }

                        if (depth == 0)
                        {
                            end = j;
                            break;
                        }
                    }

                    if (end > 0)
                    {
                        Node child = Node.Parse(pattern.Substring(i + 1, end - i - 1));
                        ret.children.Add(child);
                        i = end;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (pattern[i] == '|')
                {
                    if (current != "")
                    {
                        Node literal = new Node();
                        literal.value = current;
                        literal.type = Type.Literal;
                        ret.children.Add(literal);
                    }

                    current = "";

                    int prevSeries;
                    for (prevSeries = ret.children.Count - 1; prevSeries >= 0; prevSeries--)
                    {
                        if (ret.children[prevSeries].type == Type.Series)
                        {
                            break;
                        }
                    }

                    ret.type = Type.Choice;

                    List<Node> seriesChildren = ret.children.Skip(prevSeries + 1).TakeWhile(n => true).ToList();
                    ret.children.RemoveRange(prevSeries + 1, ret.children.Count - prevSeries - 1);
                    Node seriesChild = new Node();
                    seriesChild.type = Type.Series;
                    seriesChild.children = seriesChildren;
                    ret.children.Add(seriesChild);
                }
                else
                {
                    current += pattern[i];
                }
            }

            if (current != "")
            {
                Node literal = new Node();
                literal.value = current;
                literal.type = Type.Literal;
                ret.children.Add(literal);
            }

            if (ret.type == Type.Choice)
            {
                int prevSeries;
                for (prevSeries = ret.children.Count - 1; prevSeries >= 0; prevSeries--)
                {
                    if (ret.children[prevSeries].type == Type.Series)
                    {
                        break;
                    }
                }

                ret.type = Type.Choice;

                List<Node> seriesChildren = ret.children.Skip(prevSeries + 1).TakeWhile(n => true).ToList();
                ret.children.RemoveRange(prevSeries + 1, ret.children.Count - prevSeries - 1);
                Node seriesChild = new Node();
                seriesChild.type = Type.Series;
                seriesChild.children = seriesChildren;
                ret.children.Add(seriesChild);
            }

            return ret;
        }

        public string Generate()
        {
            switch (type)
            {
                case Type.Literal:
                    return value;
                case Type.Adverb:
                    return Choose(MainForm.Adverbs, capitalize);
                case Type.Adjective:
                    return Choose(MainForm.Adjectives, capitalize);
                case Type.Noun:
                    return Choose(MainForm.Nouns, capitalize);
                case Type.Sense:
                    return Choose(MainForm.Senses, capitalize);
                case Type.Verb:
                    return Choose(MainForm.Verbs, capitalize);
                case Type.Consonant:
                    return Choose(MainForm.Consonants, capitalize);
                case Type.Vowel:
                    return Choose(MainForm.Vowels, capitalize);
                case Type.Choice:
                    return children[MainForm.Random.Next(0, children.Count)].Generate();
                case Type.Series:
                    string ret = "";
                    children.ForEach(n => ret += n.Generate());
                    return ret;
                default:
                    return null;
            }
        }

        private string Choose(List<string> options, bool capitalize)
        {
            var str = options[MainForm.Random.Next(0, options.Count)];
            if (capitalize)
            {
                str = char.ToUpper(str[0]) + str.Substring(1);
            }

            return str;
        }

        enum Type
        {
            Literal,
            Adverb,
            Adjective,
            Noun,
            Sense,
            Verb,
            Consonant,
            Vowel,
            Choice,
            Series
        }
    }
}
