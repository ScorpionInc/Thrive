﻿using System.Collections.Generic;
using Godot;

/// <summary>
///   A page in the Thriveopedia generated by content from the online wiki.
/// </summary>
public abstract class ThriveopediaWikiPage : ThriveopediaPage
{
    [Export]
    public NodePath? MainArticlePath;

#pragma warning disable CA2213
    protected PackedScene pageSectionScene = null!;
    protected VBoxContainer mainArticle = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Wiki content to display on this page.
    /// </summary>
    public GameWiki.Page PageContent { get; set; } = null!;

    /// <summary>
    ///   Link to this page in the online wiki.
    /// </summary>
    public string Url => PageContent.Url;

    public override string PageName => PageContent.InternalName;
    public override string TranslatedPageName => TranslationServer.Translate(PageContent.Name);

    /// <summary>
    ///   Creates all wiki pages using the data in wiki.json, in order of their definition. In particular, parents must
    ///   always come before children in this list.
    /// </summary>
    public static List<ThriveopediaWikiPage> GenerateAllWikiPages()
    {
        var pages = new List<ThriveopediaWikiPage>();

        var wiki = SimulationParameters.Instance.GetWiki();

        var organellesRootScene =
            GD.Load<PackedScene>("res://src/thriveopedia/pages/wiki/ThriveopediaOrganellesRootPage.tscn");
        var organellesRootPage = (ThriveopediaOrganellesRootPage)organellesRootScene.Instance();
        organellesRootPage.PageContent = wiki.OrganellesRoot;
        pages.Add(organellesRootPage);

        var organellePageScene =
            GD.Load<PackedScene>("res://src/thriveopedia/pages/wiki/ThriveopediaOrganellePage.tscn");

        foreach (var organellePage in wiki.Organelles)
        {
            var page = (ThriveopediaOrganellePage)organellePageScene.Instance();
            page.PageContent = organellePage;
            page.Organelle = SimulationParameters.Instance.GetOrganelleType(organellePage.InternalName);
            pages.Add(page);
        }

        return pages;
    }

    public override void _Ready()
    {
        base._Ready();

        mainArticle = GetNode<VBoxContainer>(MainArticlePath);
        pageSectionScene = GD.Load<PackedScene>("res://src/thriveopedia/pages/wiki/WikiPageSection.tscn");

        foreach (var section in PageContent.Sections)
            AddSection(section);
    }

    /// <summary>
    ///   Adds a page section to the main content container in the scene.
    /// </summary>
    protected void AddSection(GameWiki.Page.Section content)
    {
        var section = (WikiPageSection)pageSectionScene.Instance();

        if (content.SectionHeading != null)
            section.HeadingText = content.SectionHeading;

        section.BodyText = content.SectionBody;
        mainArticle.AddChild(section);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (MainArticlePath != null)
            {
                MainArticlePath.Dispose();
                pageSectionScene.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}