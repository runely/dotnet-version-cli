using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace dotnet.version.changelog.CsProj;

public class ProjectFileParser
{
    public virtual string PackageName { get; private set; }

    public virtual string PackageVersion { get; private set; }

    public virtual string Version { get; private set; }

    public virtual string VersionPrefix { get; private set; }

    public virtual string VersionSuffix { get; private set; }

    public virtual ProjectFileProperty DesiredVersionSource { get; private set; }

    public ProjectFileProperty VersionSource
    {
        get
        {
            if (DesiredVersionSource == ProjectFileProperty.Version)
            {
                return string.IsNullOrWhiteSpace(Version)
                    ? ProjectFileProperty.VersionPrefix
                    : ProjectFileProperty.Version;
            }

            return ProjectFileProperty.PackageVersion;
        }
    }

    private IEnumerable<XElement> PropertyGroup { get; set; }

    protected virtual void Load(string xmlDocument, ProjectFileProperty property)
    {
        LoadPropertyGroup(xmlDocument);

        XElement propertyElement = LoadProperty(property);

        switch (property)
        {
            case ProjectFileProperty.Version:
                Version = propertyElement?.Value ?? string.Empty;
                break;
            case ProjectFileProperty.PackageVersion:
                PackageVersion = propertyElement?.Value ?? string.Empty;
                break;
            case ProjectFileProperty.Title:
                var defaultPropertyElement = LoadProperty(ProjectFileProperty.PackageId);
                PackageName = propertyElement?.Value ?? defaultPropertyElement?.Value ?? string.Empty;
                break;
            case ProjectFileProperty.VersionPrefix:
                VersionPrefix = propertyElement?.Value ?? string.Empty;
                break;
            case ProjectFileProperty.VersionSuffix:
                VersionSuffix = propertyElement?.Value ?? string.Empty;
                break;
        }
    }


    public virtual void Load(string xmlDocument, ProjectFileProperty versionSource,  params ProjectFileProperty[] properties)
    {
        DesiredVersionSource = versionSource;
            
        if (properties == null || !properties.Any())
        {
            properties = new[]
            {
                ProjectFileProperty.Title,
                ProjectFileProperty.Version,
                ProjectFileProperty.PackageId,
                ProjectFileProperty.PackageVersion,
                ProjectFileProperty.VersionPrefix,
                ProjectFileProperty.VersionSuffix
            };
        }

        // Try to load xmlDocument even if there is no properties to be loaded
        // in order to verify if project file is well formed
        LoadPropertyGroup(xmlDocument);

        foreach (var property in properties)
        {
            Load(xmlDocument, property);
        }
    }

    public string GetHumanReadableVersionFromSource()
    {
        return VersionSource switch
        {
            ProjectFileProperty.Version => Version,
            ProjectFileProperty.VersionPrefix => $"{VersionPrefix}-{VersionSuffix}",
            ProjectFileProperty.PackageVersion => PackageVersion,
            _ => throw new ArgumentOutOfRangeException($"Unknown version source {VersionSource}")
        };
    }

    private XElement LoadProperty(ProjectFileProperty property)
    {
        XElement propertyElement = (
            from prop in PropertyGroup.Elements()
            where prop.Name == property.ToString("g")
            select prop
        ).FirstOrDefault();
        return propertyElement;
    }

    private void LoadPropertyGroup(string xmlDocument)
    {
        // Check if it has been already loaded
        if (PropertyGroup != null) return;

        var xml = XDocument.Parse(xmlDocument, LoadOptions.PreserveWhitespace);

        // Project should be root of the document
        var project = xml.Elements("Project");
        var xProject = project as IList<XElement> ?? project.ToList();
        if (!xProject.Any())
        {
            throw new ArgumentException(
                "The provided csproj file seems malformed - no <Project> in the root",
                paramName: nameof(xmlDocument)
            );
        }

        PropertyGroup = xProject.Elements("PropertyGroup");
    }
}