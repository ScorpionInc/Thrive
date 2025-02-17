﻿using System;
using Godot;

/// <summary>
///   A single patch in PatchMapDrawer
/// </summary>
public class PatchMapNode : MarginContainer
{
    [Export]
    public NodePath? IconPath;

    /// <summary>
    ///   Selected patch graphics
    /// </summary>
    [Export]
    public NodePath HighlightPanelPath = null!;

    /// <summary>
    ///   Player patch graphics
    /// </summary>
    [Export]
    public NodePath MarkPanelPath = null!;

    /// <summary>
    ///   For patches adjacent to the selected one
    /// </summary>
    [Export]
    public NodePath AdjacentPanelPath = null!;

    [Export]
    public NodePath UnknownLabelPath = null!;

    [Export]
    public string UnknownTextureFilePath = null!;

    // TODO: Move this to Constants.cs
    private const float HalfBlinkInterval = 0.5f;

#pragma warning disable CA2213
    private TextureRect? iconRect;
    private Panel? highlightPanel;
    private Panel? markPanel;
    private Panel? adjacentHighlightPanel;
    private Label? unknownLabel;
#pragma warning restore CA2213

    private Texture? patchIcon;

    /// <summary>
    ///   True if mouse is hovering on this node
    /// </summary>
    private bool highlighted;

    /// <summary>
    ///   True if the current node is selected
    /// </summary>
    private bool selected;

    /// <summary>
    ///   True if player is in the current node
    /// </summary>
    private bool marked;

    /// <summary>
    ///   True if player can move to this patch
    /// </summary>
    private bool enabled = true;

    /// <summary>
    ///   True if the patch is adjacent to the selected patch
    /// </summary>
    private bool adjacentToSelectedPatch;

    private float currentBlinkTime;

    private Patch? patch;

    /// <summary>
    ///   This object does nothing with this, this is stored here to make other code simpler
    /// </summary>
    public Patch Patch
    {
        get => patch ?? throw new InvalidOperationException("Patch not set yet");
        set => patch = value;
    }

    public MapElementVisibility Visibility
    {
        get => Patch.Visibility;
        set => Patch.ApplyVisibility(value);
    }

    public bool IsDirty { get; private set; }

    public ShaderMaterial? MonochromeMaterial { get; set; }

    public Action<PatchMapNode>? SelectCallback { get; set; }

    /// <summary>
    ///   Display the icon in color and make it highlightable/selectable.
    ///   Setting this to false removes current selection.
    /// </summary>
    public bool Enabled
    {
        get => enabled;
        set
        {
            Selected = Selected && value;

            enabled = value;

            UpdateSelectHighlightRing();
            UpdateGreyscale();
        }
    }

    public Texture? PatchIcon
    {
        get => patchIcon;
        set
        {
            if (patchIcon == value)
                return;

            patchIcon = value;

            UpdateIcon();
        }
    }

    public bool Highlighted
    {
        get => highlighted;
        set
        {
            highlighted = value;

            UpdateSelectHighlightRing();
        }
    }

    public bool Selected
    {
        get => selected;
        set
        {
            selected = value;

            UpdateSelectHighlightRing();
        }
    }

    public bool Marked
    {
        get => marked;
        set
        {
            marked = value;

            UpdateMarkRing();
        }
    }

    public bool AdjacentToSelectedPatch
    {
        get => adjacentToSelectedPatch;
        set
        {
            adjacentToSelectedPatch = value;
            UpdateSelectHighlightRing();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (patch == null)
            GD.PrintErr($"{nameof(PatchMapNode)} should have {nameof(Patch)} set");

        iconRect = GetNode<TextureRect>(IconPath);
        highlightPanel = GetNode<Panel>(HighlightPanelPath);
        markPanel = GetNode<Panel>(MarkPanelPath);
        adjacentHighlightPanel = GetNode<Panel>(AdjacentPanelPath);
        unknownLabel = GetNode<Label>(UnknownLabelPath);

        UpdateSelectHighlightRing();
        UpdateMarkRing();
        UpdateIcon();
        UpdateGreyscale();

        UpdateVisibility();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        currentBlinkTime += delta;
        if (currentBlinkTime > HalfBlinkInterval)
        {
            currentBlinkTime = 0;

            if (Marked)
                markPanel!.Visible = !markPanel.Visible;
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Enabled)
            return;

        if (@event is InputEventMouseButton
            {
                Pressed: true, ButtonIndex: (int)ButtonList.Left or (int)ButtonList.Right,
            })
        {
            IsDirty = true;
            OnSelect();
            GetTree().SetInputAsHandled();
        }
    }

    public void UpdateVisibility()
    {
        if (unknownLabel == null)
            throw new InvalidOperationException("Not initialized yet");

        switch (Visibility)
        {
            case MapElementVisibility.Hidden:
                Hide();
                return;

            case MapElementVisibility.Unknown:
            {
                Show();
                unknownLabel.Show();

                // TODO: would it help anything to persistently load the unknown texture (instead of each time here)?
                PatchIcon = GD.Load<Texture>(UnknownTextureFilePath);
                return;
            }

            case MapElementVisibility.Shown:
            {
                Show();
                unknownLabel.Hide();
                PatchIcon = patch!.BiomeTemplate.LoadedIcon;
                return;
            }
        }
    }

    public void OnSelect()
    {
        Selected = true;

        SelectCallback?.Invoke(this);
    }

    public void OnMouseEnter()
    {
        Highlighted = true;
    }

    public void OnMouseExit()
    {
        Highlighted = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (IconPath != null)
            {
                IconPath.Dispose();
                HighlightPanelPath.Dispose();
                MarkPanelPath.Dispose();
                AdjacentPanelPath.Dispose();
                UnknownLabelPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateSelectHighlightRing()
    {
        if (highlightPanel == null || adjacentHighlightPanel == null)
            return;

        if (Enabled)
        {
            highlightPanel.Visible = Highlighted || Selected;
            adjacentHighlightPanel.Visible = AdjacentToSelectedPatch;
        }
        else
        {
            highlightPanel.Visible = false;
            adjacentHighlightPanel.Visible = false;
        }
    }

    private void UpdateMarkRing()
    {
        if (markPanel == null)
            return;

        markPanel.Visible = Marked;
    }

    private void UpdateIcon()
    {
        if (PatchIcon == null || iconRect == null)
            return;

        iconRect.Texture = PatchIcon;
    }

    private void UpdateGreyscale()
    {
        if (iconRect == null)
            return;

        iconRect.Material = Enabled ? null : MonochromeMaterial;
    }
}
