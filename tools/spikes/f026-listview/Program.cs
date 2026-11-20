// F026 — comparativo: lista nativa do Windows (comctl32 ListView via WinForms), modo Details, mesma pasta de 1.600 itens.
// Serve só de referencia de custo por mudanca de selecao com tecla segurada. Nao faz parte do produto.
using System.Globalization;

var folder = args.Length > 0 ? args[0] : @"C:\KyraFilesUXLab\perf\1600";
var form = new Form { Text = "ListViewBaseline - " + Path.GetFileName(folder), Width = 1100, Height = 800 };
var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = true };
list.Columns.Add("Name", 380); list.Columns.Add("Date modified", 180); list.Columns.Add("Type", 120); list.Columns.Add("Size", 120);
var status = new Label { Dock = DockStyle.Bottom, Height = 24 };
list.BeginUpdate();
foreach (var fi in new DirectoryInfo(folder).EnumerateFiles().OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
    list.Items.Add(new ListViewItem([fi.Name, fi.LastWriteTime.ToString("g", CultureInfo.CurrentCulture), fi.Extension.TrimStart('.').ToUpperInvariant() + " file", (fi.Length / 1024.0).ToString("N1") + " KB"]));
list.EndUpdate();
list.SelectedIndexChanged += (_, _) => status.Text = list.SelectedItems.Count + " selected of " + list.Items.Count;
form.Controls.Add(list); form.Controls.Add(status);
form.Shown += (_, _) => { list.Items[0].Selected = true; list.Items[0].Focused = true; list.Focus(); };
Application.Run(form);
