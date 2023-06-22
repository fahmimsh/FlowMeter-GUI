using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowMeterFactory
{
    public static class PopUp
    {
        public static int SetTag(int idtagtank, string[] tag)
        {
            string[] tagTankCopy = (string[])tag.Clone();
            using (Form popupForm = new Form())
            {
                // Set AutoSize form based on the contents of the controls
                popupForm.AutoSize = true;
                popupForm.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                // Set other Form properties
                popupForm.Text = "Set Tag";
                popupForm.StartPosition = FormStartPosition.CenterScreen;
                popupForm.FormBorderStyle = FormBorderStyle.FixedDialog; // Add border style to make it look more like a dialog box

                // Create label for ComboBox
                Label labelTag = new Label();
                labelTag.Text = "Select Tag:";
                labelTag.Location = new Point(20, 10);
                labelTag.Height = 20; labelTag.Width = 100;

                // Create ComboBox to allow user to set tag
                ComboBox cmbTag = new ComboBox();
                cmbTag.DropDownStyle = ComboBoxStyle.DropDownList;
                cmbTag.Items.AddRange(tagTankCopy);
                cmbTag.SelectedIndex = idtagtank;
                cmbTag.Location = new Point(10, 40);
                cmbTag.Width = 160; // Increase the width of the ComboBox

                // Create OK and Cancel buttons
                Button btnSet = new Button();
                btnSet.Text = "Set";
                btnSet.DialogResult = DialogResult.OK;
                btnSet.Location = new Point(20, 80);
                btnSet.Width = 75; // Decrease the width of the Set button

                Button btnCancel = new Button();
                btnCancel.Text = "Cancel";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.Location = new Point(100, 80);
                btnCancel.Width = 75; // Decrease the width of the Cancel button

                // Add controls to form
                popupForm.Controls.Add(labelTag);
                popupForm.Controls.Add(cmbTag);
                popupForm.Controls.Add(btnSet);
                popupForm.Controls.Add(btnCancel);

                // Show the form as a dialog box and get the result
                DialogResult dialogResult = popupForm.ShowDialog();

                // If OK is clicked, return the new tag index
                if (dialogResult == DialogResult.OK)
                {
                    idtagtank = cmbTag.SelectedIndex;
                }
            }
            return idtagtank;
        }

        public static double SetLiter(double initialValue)
        {
            using (Form form = new Form())
            {
                int formWidth = 220;
                int formHeight = 130;

                // Membuat numeric updown control
                NumericUpDown numericUpDown = new NumericUpDown()
                {
                    DecimalPlaces = 1,
                    Maximum = 100000,
                    TextAlign = HorizontalAlignment.Center,
                    Value = (decimal)initialValue,
                    Location = new Point(10, 30),
                    Size = new Size(formWidth - 35, 50),
                    Font = new Font("Segoe UI", 12)
                };

                // Membuat label "Set Liter"
                Label label = new Label()
                {
                    Text = "Set Liter :",
                    AutoSize = true,
                    Location = new Point(20, 2),
                    Font = new Font("Segoe UI", 12, FontStyle.Regular),
                };

                // Membuat tombol "Set"
                Button setButton = new Button()
                {
                    Text = "Set",
                    DialogResult = DialogResult.OK,
                    Size = new Size(75, 32),
                    Location = new Point(formWidth - 105, formHeight - 55),
                    Font = new Font("Segoe UI", 12, FontStyle.Regular),
                };
                setButton.Click += (s, ev) => { form.DialogResult = DialogResult.OK; };

                // Membuat tombol "Cancel"
                Button cancelButton = new Button()
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Size = new Size(75, 32),
                    Location = new Point(25, formHeight - 55),
                    Font = new Font("Segoe UI", 12, FontStyle.Regular),
                };
                cancelButton.Click += (s, ev) => { form.DialogResult = DialogResult.Cancel; };

                // Menambah kontrol ke dalam form
                form.StartPosition = FormStartPosition.CenterScreen;
                form.ClientSize = new Size(formWidth, formHeight);
                form.Controls.Add(cancelButton);
                form.Controls.Add(setButton);
                form.Controls.Add(numericUpDown);
                form.Controls.Add(label);

                DialogResult result = form.ShowDialog();
                if (result == DialogResult.OK)
                {
                    return (double)numericUpDown.Value;
                }
                else
                {
                    return initialValue;
                }
            }
        }
    }
}
