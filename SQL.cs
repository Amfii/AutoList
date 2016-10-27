using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoList
{
    class SQL
    {
        String sqlString = "user id=root;" +
                           "password=root;" +
                           "server=DESKTOP-8LBDR39\\SQLEXPRESS;" +
                           "Trusted_Connection=yes;" +
                           "database=autolist; " +
                           "connection timeout=30;" +
                           "MultipleActiveResultSets=True";

        Form1 myForm;

        public SQL(Form1 form)
        {
            myForm = form;
        }

        /// <summary>
        ///  Loads car data to DataGrid from database.
        /// </summary>
        public void LoadDataGrid()
        {
            using (SqlConnection connection = new SqlConnection(sqlString))
            using (SqlCommand command = new SqlCommand("SELECT * from Car", connection))
            {
                try
                {
                    connection.Open();
                    myForm.ClearDataGrid();
                    DataTable dt = new DataTable();
                    dt.Load(command.ExecuteReader());
                    myForm.AddToDataGrid(dt);
                    myForm.SetupColumns();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        ///  Loads data to Tree from database.
        /// </summary>
        public void LoadTree()
        {
            using (SqlConnection connection = new SqlConnection(sqlString))
            using (SqlCommand manCommand = new SqlCommand("SELECT * from Manufacturer", connection))
            {
                try
                {
                    myForm.ClearTree();
                    connection.Open();
                    using (SqlDataReader manReader = manCommand.ExecuteReader())
                    {
                        TreeNode allNode = new TreeNode("All"); // Create the parent "All" node
                        while (manReader.Read())
                        {
                            TreeNode parent = allNode.Nodes.Add(manReader["ManufacturerName"].ToString());
                            using (SqlCommand carCommand = new SqlCommand("SELECT DISTINCT CarName from Car where "+
                                "CarManufacturer = @CarManufacturer;", connection))
                            {
                                carCommand.Parameters.Add("@CarManufacturer", SqlDbType.NVarChar, 40).Value =
                                    manReader["ManufacturerName"].ToString();
                                using (SqlDataReader carReader = carCommand.ExecuteReader())
                                {
                                    while (carReader.Read())
                                    {
                                        parent.Nodes.Add(carReader["CarName"].ToString());
                                    }
                                }
                            }
                        }
                        myForm.AddToTree(allNode);
                        myForm.SortTreeView();
                        allNode.Expand();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        ///  Inserts a new manufacturer to database and TreeView control.
        /// </summary>
        public void AddManufacturer()
        {
            using (SqlConnection connection = new SqlConnection(sqlString))
            using (SqlCommand command = new SqlCommand("INSERT INTO Manufacturer (ManufacturerName)"+
                "VALUES(@ManufacturerName)", connection))
            {
                command.Parameters.Add("@ManufacturerName", SqlDbType.NVarChar, 40).Value = "";
                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                    TreeNode newManufacturer = new TreeNode("");
                    myForm.AddManufacturerToTree(newManufacturer);
                    newManufacturer.BeginEdit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        ///  Updates data in the database with changes made in DataTable.
        /// </summary>
        public void UpdateToDatabase(DataTable table)
        {
            using (SqlConnection connection = new SqlConnection(sqlString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Car", connection))
            {
                try
                {
                    connection.Open();

                    // Delete command
                    SqlCommand command = new SqlCommand(
                        "DELETE FROM Car WHERE CarID = @CarID", connection);
                    SqlParameter parameter = command.Parameters.Add(
                        "@CarID", SqlDbType.NChar,5, "CarID");
                    parameter.SourceVersion = DataRowVersion.Original;
                    adapter.DeleteCommand = command;

                    // Insert command
                    command = new SqlCommand(
                        "INSERT INTO Car (CarManufacturer, CarName, CarEngine, CarYear, CarColor, CarPrice) " +
                        "VALUES (@CarManufacturer, @CarName, @CarEngine, @CarYear, @CarColor, @CarPrice)", connection);
                    command.Parameters.Add("@CarManufacturer", SqlDbType.NVarChar, 40, "CarManufacturer");
                    command.Parameters.Add("@CarName", SqlDbType.NVarChar, 40, "CarName");
                    command.Parameters.Add("@CarEngine", SqlDbType.NVarChar, 40, "CarEngine");
                    command.Parameters.Add("@CarYear", SqlDbType.NChar, 5, "CarYear");
                    command.Parameters.Add("@CarColor", SqlDbType.NVarChar, 40, "CarColor");
                    command.Parameters.Add("@CarPrice", SqlDbType.NChar, 8, "CarPrice");
                    adapter.InsertCommand = command;

                    // Update command
                    command = new SqlCommand(
                        "UPDATE Car SET CarManufacturer = @CarManufacturer, CarName = @CarName, CarEngine = @CarEngine, " +
                        "CarYear = @CarYear, CarColor = @CarColor, CarPrice = @CarPrice WHERE CarID = @oldCarID", connection);
                    command.Parameters.Add("@CarManufacturer", SqlDbType.NVarChar, 40, "CarManufacturer");
                    command.Parameters.Add("@CarName", SqlDbType.NVarChar, 40, "CarName");
                    command.Parameters.Add("@CarEngine", SqlDbType.NVarChar, 40, "CarEngine");
                    command.Parameters.Add("@CarYear", SqlDbType.NChar, 5, "CarYear");
                    command.Parameters.Add("@CarColor", SqlDbType.NVarChar, 40, "CarColor");
                    command.Parameters.Add("@CarPrice", SqlDbType.NChar, 8, "CarPrice");
                    SqlParameter updateParameter = command.Parameters.Add(
                        "@oldCarID", SqlDbType.NChar, 5, "CarID");
                    updateParameter.SourceVersion = DataRowVersion.Original;
                    adapter.UpdateCommand = command;

                    adapter.Update(table);
                    LoadTree(); // Update Tree with new car data
                }
                catch (Exception)
                {
                    MessageBox.Show("An error occured while trying to save", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        ///  Updates TreeNode changes to database.
        /// </summary>
        public void EditNode(NodeLabelEditEventArgs e)
        {
            String tableName;
            String columnName;
            
            // Check whether user is updating manufacturer or car
            if (e.Node.Parent.Text.Equals("All"))
            {
                tableName = "Manufacturer";
                columnName = "ManufacturerName";
            } else
            {
                tableName = "Car";
                columnName = "CarName";
            }

            using (SqlConnection connection = new SqlConnection(sqlString))
            using (SqlCommand command = new SqlCommand("UPDATE " + tableName + " SET " + columnName + 
                "= @columnValue WHERE " + columnName + "= @oldColumnValue;", connection))
            {
                command.Parameters.Add("@columnValue", SqlDbType.NVarChar, 40).Value = e.Label;
                command.Parameters.Add("@oldColumnValue", SqlDbType.NVarChar, 40).Value = e.Node.Text;
                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                    e.Node.EndEdit(false);
                    LoadDataGrid();
                }
                catch (Exception)
                {
                    e.CancelEdit = true;
                    MessageBox.Show("An error occured while trying to save", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        ///  Deletes TreeNode from database.
        /// </summary>
        public void DeleteTreeViewNode(TreeNode node)
        {
            String tableName;
            String columnName;

            // Check whether user is deleting manufacturer or car
            if (node.Parent.Text.Equals("All"))
            {
                tableName = "Manufacturer";
                columnName = "ManufacturerName";
            }
            else
            {
                tableName = "Car";
                columnName = "CarName";
            }

            using (SqlConnection connection = new SqlConnection(sqlString))
            using (SqlCommand command = new SqlCommand(
                "DELETE FROM " + tableName + " WHERE " + columnName + " = @columnValue;", connection))
            {
                command.Parameters.Add("@columnValue", SqlDbType.NVarChar, 40).Value = node.Text;
                try
                {
                    connection.Open();
                    if (node.Nodes.Count > 0) // If manufacturer has cars delete them too
                    {
                        using (SqlCommand childCommand = new SqlCommand(
                            "DELETE FROM Car WHERE CarManufacturer = @CarManufacturer;", connection))
                        {
                            childCommand.Parameters.Add("@CarManufacturer", SqlDbType.NVarChar, 40).Value = node.Text;
                            childCommand.ExecuteNonQuery();
                        }
                    }
                    command.ExecuteNonQuery();
                    node.Remove();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        ///  Check if manufacturer exists in database.
        /// </summary>
        public bool containsManufacturer(String manufacturer)
        {
            using (SqlConnection connection = new SqlConnection(sqlString))
            using (SqlCommand command = new SqlCommand(
                "SELECT COUNT(1) FROM Manufacturer WHERE ManufacturerName = @ManufacturerName;", connection))
            {
                command.Parameters.Add("@ManufacturerName", SqlDbType.NVarChar, 40).Value = manufacturer;
                try
                {
                    connection.Open();
                    if ((int) command.ExecuteScalar() > 0) // Manufacturer exists in database
                        return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    connection.Close();
                }
            }

                return false;
        }
    }
}
