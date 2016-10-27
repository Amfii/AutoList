using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;

namespace AutoList
{
    public partial class Form1 : Form
    {
        StringFormat strFormat;                         //Used to format the grid rows.
        ArrayList arrColumnLefts = new ArrayList();     //Used to save left coordinates of columns
        ArrayList arrColumnWidths = new ArrayList();    //Used to save column widths
        int iCellHeight = 0;                            //Used to get/set the datagridview cell height
        int iTotalWidth = 0; 
        int iRow = 0;                                   //Used as counter
        bool bFirstPage = false;                        //Used to check whether we are printing first page
        bool bNewPage = false;                          //Used to check whether we are printing a new page
        int iHeaderHeight = 0;                          //Used for the header height

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        ///  Adds a node to treeview1 control.
        /// </summary>
        public void AddToTree(TreeNode node)
        {
            treeView1.Nodes.Add(node);
        }

        /// <summary>
        ///  Adds new manufacturer to treeview1 control
        /// </summary>
        public void AddManufacturerToTree(TreeNode node)
        {
            if (treeView1.Nodes.Count > 0)
            {
                treeView1.Nodes[0].Nodes.Add(node); // Assign it to "All" node
                treeView1.Nodes[0].Expand();
            }
        }

        /// <summary>
        ///  Filters dataGridView1 controls column by filterValue
        /// </summary>
        private void FilterDataGrid(String columnName, String filterValue)
        {
            String rowFilter = String.Format("[{0}] = '{1}'", columnName, filterValue);
            (dataGridView1.DataSource as DataTable).DefaultView.RowFilter = rowFilter;
        }

        /// <summary>
        ///  Assigns DataTable to dataGridView1
        /// </summary>
        public void AddToDataGrid(DataTable table)
        {
            dataGridView1.DataSource = table;
        }

        /// <summary>
        ///  Calls the Sort() method for treeView1 control
        /// </summary>
        public void SortTreeView()
        {
            treeView1.Sort();
        }

        /// <summary>
        ///  Calls the Clear() method for treeView1 control
        /// </summary>
        public void ClearTree()
        {
            treeView1.Nodes.Clear();
        }

        /// <summary>
        ///  Sets DataSource for dataGridViw1 control to null
        /// </summary>
        public void ClearDataGrid()
        {
            dataGridView1.DataSource = null;
        }

        /// <summary>
        ///  Sets the width and Header Text for dataGridViw1 control columns
        /// </summary>
        public void SetupColumns()
        {
            if (dataGridView1.Columns.Count > 0)
            {
                dataGridView1.Columns[0].Width = 50;
                dataGridView1.Columns[4].Width = 70;
                dataGridView1.Columns[5].Width = 80;
                dataGridView1.Columns[0].HeaderText = "ID";
                dataGridView1.Columns[1].HeaderText = "Manufacturer";
                dataGridView1.Columns[2].HeaderText = "Car Name";
                dataGridView1.Columns[3].HeaderText = "Engine";
                dataGridView1.Columns[4].HeaderText = "Year";
                dataGridView1.Columns[5].HeaderText = "Color";
                dataGridView1.Columns[6].HeaderText = "Price, €";
            }
        }

        private void manufacturerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SQL sql = new SQL(this);
            sql.AddManufacturer();
        }

        private void treeView1_NodeMouseClick(object sender, EventArgs e)
        {
            SQL sql = new SQL(this);
            sql.LoadTree();
            sql.LoadDataGrid();
        }

        private void newManufacturerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SQL sql = new SQL(this);
            sql.AddManufacturer();
        }

        private void treeView1_AfterLabelEdit_1(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Label != null)
            {
                if (e.Label.Length > 0)
                {
                    SQL sql = new SQL(this);
                    if (!sql.containsManufacturer(e.Label))
                    {
                        if (e.Label.IndexOfAny(new char[] { '@', '.', ',', '!' }) == -1 && !e.Label.Equals("All"))
                            sql.EditNode(e);
                        else
                        {
                            /* Cancel the label edit action, inform the user, and 
                               place the node in edit mode again. */
                            e.CancelEdit = true;
                            MessageBox.Show("Invalid manufacturer label.\n" +
                               "The invalid characters are: '@','.', ',', '!'",
                               "Manufacturer Edit");
                            e.Node.BeginEdit();
                        }
                    } else
                    {
                        /* Cancel the label edit action, inform the user, and 
                               place the node in edit mode again. */
                        e.CancelEdit = true;
                        MessageBox.Show("Invalid manufacturer label.\n" +
                           "Manufacturer by this name already exists",
                           "Manufacturer Edit");
                        e.Node.BeginEdit();
                    }
                }
                else
                {
                    /* Cancel the label edit action, inform the user, and 
                       place the node in edit mode again. */
                    e.CancelEdit = true;
                    MessageBox.Show("Invalid manufacturer label.\nThe label cannot be blank",
                       "Manufacturer Edit");
                    e.Node.BeginEdit();
                }
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                if (!treeView1.SelectedNode.Text.Equals("All"))
                {
                    if (treeView1.SelectedNode.Nodes.Count > 0)
                    {
                        // Give user confirmation prompt if manufacturer has cars
                        var confirmResult = MessageBox.Show("All manufacturer cars will be deleted. Are you sure?",
                                     "Confirm Delete",
                                     MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (confirmResult == DialogResult.Yes)
                        {
                            SQL sql = new SQL(this);
                            sql.DeleteTreeViewNode(treeView1.SelectedNode);
                            sql.LoadDataGrid();
                        }
                    }
                    else
                    {
                        SQL sql = new SQL(this);
                        sql.DeleteTreeViewNode(treeView1.SelectedNode);
                        sql.LoadDataGrid();
                    }
                }
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (dataGridView1.DataSource != null)
            {
                if (e.Node.Parent == null) // Remove filter if clicked "All" node
                    (dataGridView1.DataSource as DataTable).DefaultView.RowFilter = null;
                else if (e.Node.Parent.Text.Equals("All"))
                    FilterDataGrid(dataGridView1.Columns[1].Name, e.Node.Text);
                else
                    FilterDataGrid(dataGridView1.Columns[2].Name, e.Node.Text);
            }
        }

        private void printDocument1_BeginPrint(object sender, System.Drawing.Printing.PrintEventArgs e)
        {
            try
            {
                strFormat = new StringFormat();
                strFormat.Alignment = StringAlignment.Near;
                strFormat.LineAlignment = StringAlignment.Center;
                strFormat.Trimming = StringTrimming.EllipsisCharacter;

                arrColumnLefts.Clear();
                arrColumnWidths.Clear();
                iCellHeight = 0;
                iRow = 0;
                bFirstPage = true;
                bNewPage = true;

                // Calculating Total Widths
                iTotalWidth = 0;
                foreach (DataGridViewColumn dgvGridCol in dataGridView1.Columns)
                {
                    iTotalWidth += dgvGridCol.Width;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            try
            {
                //Set the left margin
                int iLeftMargin = e.MarginBounds.Left;
                //Set the top margin
                int iTopMargin = e.MarginBounds.Top;
                //Whether more pages have to print or not
                bool bMorePagesToPrint = false;
                int iTmpWidth = 0;

                //For the first page to print set the cell width and header height
                if (bFirstPage)
                {
                    foreach (DataGridViewColumn GridCol in dataGridView1.Columns)
                    {
                        iTmpWidth = (int)(Math.Floor((double)((double)GridCol.Width /
                                       (double)iTotalWidth * (double)iTotalWidth *
                                       ((double)e.MarginBounds.Width / (double)iTotalWidth))));

                        iHeaderHeight = (int)(e.Graphics.MeasureString(GridCol.HeaderText,
                                    GridCol.InheritedStyle.Font, iTmpWidth).Height) + 11;

                        // Save width and height of headres
                        arrColumnLefts.Add(iLeftMargin);
                        arrColumnWidths.Add(iTmpWidth);
                        iLeftMargin += iTmpWidth;
                    }
                }
                //Loop till all the grid rows not get printed
                while (iRow <= dataGridView1.Rows.Count - 1)
                {
                    DataGridViewRow GridRow = dataGridView1.Rows[iRow];
                    //Set the cell height
                    iCellHeight = GridRow.Height + 5;
                    int iCount = 0;
                    //Check whether the current page settings allo more rows to print
                    if (iTopMargin + iCellHeight >= e.MarginBounds.Height + e.MarginBounds.Top)
                    {
                        bNewPage = true;
                        bFirstPage = false;
                        bMorePagesToPrint = true;
                        break;
                    }
                    else
                    {
                        if (bNewPage)
                        {
                            //Draw Header
                            e.Graphics.DrawString("Car Summary", new Font(dataGridView1.Font, FontStyle.Bold),
                                    Brushes.Black, e.MarginBounds.Left, e.MarginBounds.Top -
                                    e.Graphics.MeasureString("Car Summary", new Font(dataGridView1.Font,
                                    FontStyle.Bold), e.MarginBounds.Width).Height - 13);

                            String strDate = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToShortTimeString();
                            //Draw Date
                            e.Graphics.DrawString(strDate, new Font(dataGridView1.Font, FontStyle.Bold),
                                    Brushes.Black, e.MarginBounds.Left + (e.MarginBounds.Width -
                                    e.Graphics.MeasureString(strDate, new Font(dataGridView1.Font,
                                    FontStyle.Bold), e.MarginBounds.Width).Width), e.MarginBounds.Top -
                                    e.Graphics.MeasureString("Car Summary", new Font(new Font(dataGridView1.Font,
                                    FontStyle.Bold), FontStyle.Bold), e.MarginBounds.Width).Height - 13);

                            //Draw Columns                 
                            iTopMargin = e.MarginBounds.Top;
                            foreach (DataGridViewColumn GridCol in dataGridView1.Columns)
                            {
                                e.Graphics.FillRectangle(new SolidBrush(Color.LightGray),
                                    new Rectangle((int)arrColumnLefts[iCount], iTopMargin,
                                    (int)arrColumnWidths[iCount], iHeaderHeight));

                                e.Graphics.DrawRectangle(Pens.Black,
                                    new Rectangle((int)arrColumnLefts[iCount], iTopMargin,
                                    (int)arrColumnWidths[iCount], iHeaderHeight));

                                e.Graphics.DrawString(GridCol.HeaderText, GridCol.InheritedStyle.Font,
                                    new SolidBrush(GridCol.InheritedStyle.ForeColor),
                                    new RectangleF((int)arrColumnLefts[iCount], iTopMargin,
                                    (int)arrColumnWidths[iCount], iHeaderHeight), strFormat);
                                iCount++;
                            }
                            bNewPage = false;
                            iTopMargin += iHeaderHeight;
                        }
                        iCount = 0;
                        //Draw Columns Contents                
                        foreach (DataGridViewCell Cel in GridRow.Cells)
                        {
                            if (Cel.Value != null)
                            {
                                e.Graphics.DrawString(Cel.Value.ToString(), Cel.InheritedStyle.Font,
                                            new SolidBrush(Cel.InheritedStyle.ForeColor),
                                            new RectangleF((int)arrColumnLefts[iCount], (float)iTopMargin,
                                            (int)arrColumnWidths[iCount], (float)iCellHeight), strFormat);
                            }
                            //Drawing Cells Borders 
                            e.Graphics.DrawRectangle(Pens.Black, new Rectangle((int)arrColumnLefts[iCount],
                                    iTopMargin, (int)arrColumnWidths[iCount], iCellHeight));

                            iCount++;
                        }
                    }
                    iRow++;
                    iTopMargin += iCellHeight;
                }

                //If more lines exist, print another page.
                if (bMorePagesToPrint)
                    e.HasMorePages = true;
                else
                    e.HasMorePages = false;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void printToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Open the print dialog
            PrintDialog printDialog = new PrintDialog();
            printDialog.Document = printDocument1;
            printDialog.UseEXDialog = true;
            //Get the document
            if (DialogResult.OK == printDialog.ShowDialog())
            {
                printDocument1.DocumentName = "AutoList Print";
                printDocument1.Print();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SQL sql = new SQL(this);
            sql.UpdateToDatabase(dataGridView1.DataSource as DataTable);
        }

        private void treeView1_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Node.Text.Equals("All")) // Dont allow edit of "All" TreeNode
                e.CancelEdit = true;
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow item in this.dataGridView1.SelectedRows)
                dataGridView1.Rows.RemoveAt(item.Index);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void dataGridView1_DataError_1(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show("Error occured while saving data", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void editManufacturerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                if (!treeView1.SelectedNode.Text.Equals("All"))
                {
                    treeView1.SelectedNode.BeginEdit();
                }
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Autolist v1.0 \n" +
                            "Deividas Dunda 2016", "About",MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}