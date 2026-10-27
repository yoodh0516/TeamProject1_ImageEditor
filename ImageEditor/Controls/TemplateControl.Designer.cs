namespace ImageEditor.Controls
{
    partial class TemplateControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            label1 = new Label();
            lblSubTitle = new Label();
            btnPolaroid = new Button();
            btnFilm = new Button();
            btnMagazine = new Button();
            btnPhotoCard = new Button();
            btnInstagram = new Button();
            btnBirthday = new Button();
            btnPostcard = new Button();
            btnMinimalPoster = new Button();
            btnTemplateReset = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("맑은 고딕", 14F, FontStyle.Bold, GraphicsUnit.Point);
            label1.ForeColor = Color.FromArgb(38, 38, 46);
            label1.Location = new Point(18, 17);
            label1.Name = "label1";
            label1.Size = new Size(69, 25);
            label1.TabIndex = 0;
            label1.Text = "템플릿";
            // 
            // lblSubTitle
            // 
            lblSubTitle.AutoSize = true;
            lblSubTitle.Font = new Font("맑은 고딕", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            lblSubTitle.ForeColor = Color.FromArgb(120, 120, 132);
            lblSubTitle.Location = new Point(19, 49);
            lblSubTitle.Name = "lblSubTitle";
            lblSubTitle.Size = new Size(223, 15);
            lblSubTitle.TabIndex = 1;
            lblSubTitle.Text = "사진에 어울리는 레이아웃을 선택하세요";
            // 
            // btnPolaroid
            // 
            btnPolaroid.BackColor = Color.White;
            btnPolaroid.Cursor = Cursors.Hand;
            btnPolaroid.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232);
            btnPolaroid.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255);
            btnPolaroid.FlatStyle = FlatStyle.Flat;
            btnPolaroid.Font = new Font("맑은 고딕", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnPolaroid.ForeColor = Color.FromArgb(70, 64, 90);
            btnPolaroid.Location = new Point(18, 84);
            btnPolaroid.Name = "btnPolaroid";
            btnPolaroid.Size = new Size(108, 82);
            btnPolaroid.TabIndex = 2;
            btnPolaroid.Text = "▣\r\n폴라로이드";
            btnPolaroid.UseVisualStyleBackColor = false;
            btnPolaroid.Click += btnPolaroid_Click;
            // 
            // btnFilm
            // 
            btnFilm.BackColor = Color.White;
            btnFilm.Cursor = Cursors.Hand;
            btnFilm.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232);
            btnFilm.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255);
            btnFilm.FlatStyle = FlatStyle.Flat;
            btnFilm.Font = new Font("맑은 고딕", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnFilm.ForeColor = Color.FromArgb(70, 64, 90);
            btnFilm.Location = new Point(138, 84);
            btnFilm.Name = "btnFilm";
            btnFilm.Size = new Size(108, 82);
            btnFilm.TabIndex = 3;
            btnFilm.Text = "▥\r\n필름";
            btnFilm.UseVisualStyleBackColor = false;
            btnFilm.Click += btnFilm_Click;
            // 
            // btnMagazine
            // 
            btnMagazine.BackColor = Color.White;
            btnMagazine.Cursor = Cursors.Hand;
            btnMagazine.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232);
            btnMagazine.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255);
            btnMagazine.FlatStyle = FlatStyle.Flat;
            btnMagazine.Font = new Font("맑은 고딕", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnMagazine.ForeColor = Color.FromArgb(70, 64, 90);
            btnMagazine.Location = new Point(18, 178);
            btnMagazine.Name = "btnMagazine";
            btnMagazine.Size = new Size(108, 82);
            btnMagazine.TabIndex = 4;
            btnMagazine.Text = "▤\r\n매거진";
            btnMagazine.UseVisualStyleBackColor = false;
            btnMagazine.Click += btnMagazine_Click;
            // 
            // btnPhotoCard
            // 
            btnPhotoCard.BackColor = Color.White;
            btnPhotoCard.Cursor = Cursors.Hand;
            btnPhotoCard.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232);
            btnPhotoCard.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255);
            btnPhotoCard.FlatStyle = FlatStyle.Flat;
            btnPhotoCard.Font = new Font("맑은 고딕", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnPhotoCard.ForeColor = Color.FromArgb(70, 64, 90);
            btnPhotoCard.Location = new Point(138, 178);
            btnPhotoCard.Name = "btnPhotoCard";
            btnPhotoCard.Size = new Size(108, 82);
            btnPhotoCard.TabIndex = 5;
            btnPhotoCard.Text = "▱\r\n포토카드";
            btnPhotoCard.UseVisualStyleBackColor = false;
            btnPhotoCard.Click += btnPhotoCard_Click;
            // 
            // btnInstagram
            // 
            btnInstagram.BackColor = Color.White;
            btnInstagram.Cursor = Cursors.Hand;
            btnInstagram.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232);
            btnInstagram.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255);
            btnInstagram.FlatStyle = FlatStyle.Flat;
            btnInstagram.Font = new Font("맑은 고딕", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnInstagram.ForeColor = Color.FromArgb(70, 64, 90);
            btnInstagram.Location = new Point(18, 272);
            btnInstagram.Name = "btnInstagram";
            btnInstagram.Size = new Size(108, 82);
            btnInstagram.TabIndex = 6;
            btnInstagram.Text = "▦\r\n인스타그램";
            btnInstagram.UseVisualStyleBackColor = false;
            btnInstagram.Click += btnInstagram_Click;
            // 
            // btnBirthday
            // 
            btnBirthday.BackColor = Color.White;
            btnBirthday.Cursor = Cursors.Hand;
            btnBirthday.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232);
            btnBirthday.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255);
            btnBirthday.FlatStyle = FlatStyle.Flat;
            btnBirthday.Font = new Font("맑은 고딕", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnBirthday.ForeColor = Color.FromArgb(70, 64, 90);
            btnBirthday.Location = new Point(138, 272);
            btnBirthday.Name = "btnBirthday";
            btnBirthday.Size = new Size(108, 82);
            btnBirthday.TabIndex = 7;
            btnBirthday.Text = "★\r\n생일카드";
            btnBirthday.UseVisualStyleBackColor = false;
            btnBirthday.Click += btnBirthday_Click;
            // 
            // btnPostcard
            // 
            btnPostcard.BackColor = Color.White;
            btnPostcard.Cursor = Cursors.Hand;
            btnPostcard.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232);
            btnPostcard.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255);
            btnPostcard.FlatStyle = FlatStyle.Flat;
            btnPostcard.Font = new Font("맑은 고딕", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnPostcard.ForeColor = Color.FromArgb(70, 64, 90);
            btnPostcard.Location = new Point(18, 366);
            btnPostcard.Name = "btnPostcard";
            btnPostcard.Size = new Size(108, 82);
            btnPostcard.TabIndex = 8;
            btnPostcard.Text = "✈\r\n여행엽서";
            btnPostcard.UseVisualStyleBackColor = false;
            btnPostcard.Click += btnPostcard_Click;
            // 
            // btnMinimalPoster
            // 
            btnMinimalPoster.BackColor = Color.White;
            btnMinimalPoster.Cursor = Cursors.Hand;
            btnMinimalPoster.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232);
            btnMinimalPoster.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255);
            btnMinimalPoster.FlatStyle = FlatStyle.Flat;
            btnMinimalPoster.Font = new Font("맑은 고딕", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnMinimalPoster.ForeColor = Color.FromArgb(70, 64, 90);
            btnMinimalPoster.Location = new Point(138, 366);
            btnMinimalPoster.Name = "btnMinimalPoster";
            btnMinimalPoster.Size = new Size(108, 82);
            btnMinimalPoster.TabIndex = 9;
            btnMinimalPoster.Text = "▯\r\n미니멀 포스터";
            btnMinimalPoster.UseVisualStyleBackColor = false;
            btnMinimalPoster.Click += btnMinimalPoster_Click;
            // 
            // btnTemplateReset
            // 
            btnTemplateReset.BackColor = Color.FromArgb(98, 78, 210);
            btnTemplateReset.Cursor = Cursors.Hand;
            btnTemplateReset.FlatAppearance.BorderSize = 0;
            btnTemplateReset.FlatStyle = FlatStyle.Flat;
            btnTemplateReset.Font = new Font("맑은 고딕", 9F, FontStyle.Bold, GraphicsUnit.Point);
            btnTemplateReset.ForeColor = Color.White;
            btnTemplateReset.Location = new Point(18, 463);
            btnTemplateReset.Name = "btnTemplateReset";
            btnTemplateReset.Size = new Size(228, 38);
            btnTemplateReset.TabIndex = 10;
            btnTemplateReset.Text = "템플릿 초기화";
            btnTemplateReset.UseVisualStyleBackColor = false;
            btnTemplateReset.Click += btnTemplateReset_Click;
            // 
            // TemplateControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            BackColor = Color.FromArgb(247, 247, 250);
            Controls.Add(btnTemplateReset);
            Controls.Add(btnMinimalPoster);
            Controls.Add(btnPostcard);
            Controls.Add(btnBirthday);
            Controls.Add(btnInstagram);
            Controls.Add(btnPhotoCard);
            Controls.Add(btnMagazine);
            Controls.Add(btnFilm);
            Controls.Add(btnPolaroid);
            Controls.Add(lblSubTitle);
            Controls.Add(label1);
            Name = "TemplateControl";
            Size = new Size(264, 518);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label lblSubTitle;
        private Button btnPolaroid;
        private Button btnFilm;
        private Button btnMagazine;
        private Button btnPhotoCard;
        private Button btnInstagram;
        private Button btnBirthday;
        private Button btnPostcard;
        private Button btnMinimalPoster;
        private Button btnTemplateReset;
    }
}
