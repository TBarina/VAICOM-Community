// File: ZoomPictureBox.cs

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class ZoomPictureBox : PictureBox
{
    // Proprietà pubbliche per lo stato della vista
    public float CurrentZoom { get; private set; }
    public PointF PanLocation { get; private set; }
    public bool IsInFitMode { get; private set; }

    // **MODIFICA**: Sovrascriviamo la proprietà Image per intercettare quando viene cambiata
    public new Image Image
    {
        get { return base.Image; }
        set
        {
            base.Image = value;
            ResetView(); // Resetta la vista ogni volta che l'immagine viene impostata
        }
    }

    public ZoomPictureBox()
    {
        // Abilita il double buffering per un rendering più fluido
        this.DoubleBuffered = true;
        ResetView();
    }

    /// <summary>
    /// Calcola la scala necessaria per adattare l'immagine al controllo.
    /// </summary>
    private float GetFitScale()
    {
        if (base.Image == null || base.Image.Width == 0 || base.Image.Height == 0) return 1.0f;
        return Math.Min((float)ClientSize.Width / base.Image.Width, (float)ClientSize.Height / base.Image.Height);
    }

    /// <summary>
    /// Ottiene la matrice di trasformazione corrente per il disegno.
    /// </summary>
    private Matrix GetTransformMatrix()
    {
        float scale = IsInFitMode ? GetFitScale() : CurrentZoom;
        Matrix matrix = new Matrix();

        if (IsInFitMode)
        {
            // In modalità "fit", centra l'immagine
            float scaledWidth = base.Image.Width * scale;
            float scaledHeight = base.Image.Height * scale;
            float offsetX = (ClientSize.Width - scaledWidth) / 2f;
            float offsetY = (ClientSize.Height - scaledHeight) / 2f;
            matrix.Translate(offsetX, offsetY);
        }
        else
        {
            // In modalità "zoom", applica il pan
            matrix.Translate(PanLocation.X, PanLocation.Y);
        }

        matrix.Scale(scale, scale);
        return matrix;
    }

    /// <summary>
    /// Converte le coordinate del controllo (schermo) in coordinate dell'immagine originale.
    /// </summary>
    public PointF ScreenToImage(Point screenPoint)
    {
        using (Matrix matrix = GetTransformMatrix())
        {
            matrix.Invert();
            PointF[] points = { screenPoint };
            matrix.TransformPoints(points);
            return points[0];
        }
    }

    /// <summary>
    /// Applica un incremento/decremento di zoom, mantenendo fisso il punto sotto il cursore.
    /// </summary>
    public void ApplyZoom(float zoomDelta, Point controlPoint)
    {
        if (base.Image == null) return;

        PointF imagePointBeforeZoom = ScreenToImage(controlPoint);

        if (IsInFitMode)
        {
            CurrentZoom = GetFitScale();
            float scaledWidth = base.Image.Width * CurrentZoom;
            float scaledHeight = base.Image.Height * CurrentZoom;
            PanLocation = new PointF(
                (ClientSize.Width - scaledWidth) / 2f,
                (ClientSize.Height - scaledHeight) / 2f
            );
            IsInFitMode = false;
        }

        float newZoom = CurrentZoom * (1 + zoomDelta);
        CurrentZoom = Math.Max(GetFitScale() / 4.0f, Math.Min(10.0f, newZoom));

        float newPanX = controlPoint.X - (imagePointBeforeZoom.X * CurrentZoom);
        float newPanY = controlPoint.Y - (imagePointBeforeZoom.Y * CurrentZoom);
        PanLocation = new PointF(newPanX, newPanY);

        Invalidate();
    }

    /// <summary>
    /// Sposta l'immagine (pan).
    /// </summary>
    public void ApplyPan(int deltaX, int deltaY)
    {
        if (IsInFitMode) return;
        PanLocation = new PointF(PanLocation.X + deltaX, PanLocation.Y + deltaY);
        Invalidate();
    }

    /// <summary>
    /// Resetta la vista alla modalità "fit-to-screen".
    /// </summary>
    public void ResetView()
    {
        IsInFitMode = true;
        CurrentZoom = 1.0f;
        PanLocation = PointF.Empty;
        if (this.IsHandleCreated) Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (base.Image == null)
        {
            e.Graphics.Clear(this.BackColor);
            return;
        }

        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

        using (Matrix transform = GetTransformMatrix())
        {
            e.Graphics.Transform = transform;
            e.Graphics.DrawImage(base.Image, 0, 0);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ResetView();
    }
}