using System;

namespace RoslynRules.Models
{
    /// <summary>
    /// Represents a semantic version for rules and workflows.
    /// Supports major.minor.patch format with optional prerelease and build metadata.
    /// Used for rule versioning, migration tracking, and compatibility checks.
    /// </summary>
    public readonly record struct RuleVersion : IComparable<RuleVersion>, IEquatable<RuleVersion>
    {
        /// <summary>
        /// Major version number. Incremented for breaking changes.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Minor version number. Incremented for new features (backward compatible).
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Patch version number. Incremented for bug fixes (backward compatible).
        /// </summary>
        public int Patch { get; }

        /// <summary>
        /// Optional prerelease identifier (e.g., "alpha", "beta.2").
        /// </summary>
        public string? Prerelease { get; }

        /// <summary>
        /// Optional build metadata (e.g., "20240101.1", "sha.abc123").
        /// Ignored in version comparison.
        /// </summary>
        public string? BuildMetadata { get; }

        /// <summary>
        /// The version that represents no version set.
        /// </summary>
        public static RuleVersion Unspecified => new(0, 0, 0);

        /// <summary>
        /// Creates a new version with major.minor.patch.
        /// </summary>
        public RuleVersion(int major, int minor = 0, int patch = 0, string? prerelease = null, string? buildMetadata = null)
        {
            if (major < 0) throw new ArgumentOutOfRangeException(nameof(major), "Major version must be >= 0.");
            if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor), "Minor version must be >= 0.");
            if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch), "Patch version must be >= 0.");

            Major = major;
            Minor = minor;
            Patch = patch;
            Prerelease = prerelease;
            BuildMetadata = buildMetadata;
        }

        /// <summary>
        /// Parses a version string in SemVer 2.0.0 format.
        /// </summary>
        /// <param name="version">Version string like "1.2.3", "1.2.3-alpha", "1.2.3-alpha+build.123"</param>
        /// <returns>Parsed RuleVersion.</returns>
        /// <exception cref="FormatException">Thrown when the version string is invalid.</exception>
        public static RuleVersion Parse(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new FormatException("Version string cannot be null or empty.");

            var prerelease = (string?)null;
            var buildMetadata = (string?)null;

            // Extract build metadata (after +)
            var plusIndex = version.IndexOf('+');
            if (plusIndex >= 0)
            {
                buildMetadata = version[(plusIndex + 1)..];
                version = version[..plusIndex];
            }

            // Extract prerelease (after -)
            var dashIndex = version.IndexOf('-');
            if (dashIndex >= 0)
            {
                prerelease = version[(dashIndex + 1)..];
                version = version[..dashIndex];
            }

            var parts = version.Split('.');
            if (parts.Length < 1 || parts.Length > 3)
                throw new FormatException($"Version must have 1-3 numeric components, found {parts.Length}.");

            if (!int.TryParse(parts[0], out var major))
                throw new FormatException($"Invalid major version: '{parts[0]}'.");

            var minor = 0;
            var patch = 0;

            if (parts.Length > 1 && !int.TryParse(parts[1], out minor))
                throw new FormatException($"Invalid minor version: '{parts[1]}'.");

            if (parts.Length > 2 && !int.TryParse(parts[2], out patch))
                throw new FormatException($"Invalid patch version: '{parts[2]}'.");

            return new RuleVersion(major, minor, patch, prerelease, buildMetadata);
        }

        /// <summary>
        /// Attempts to parse a version string.
        /// </summary>
        /// <param name="version">Version string to parse.</param>
        /// <param name="result">Parsed version if successful.</param>
        /// <returns>True if parsing succeeded.</returns>
        public static bool TryParse(string version, out RuleVersion result)
        {
            try
            {
                result = Parse(version);
                return true;
            }
            catch
            {
                result = Unspecified;
                return false;
            }
        }

        /// <summary>
        /// Compares two versions. Prerelease versions sort lower than release versions.
        /// Build metadata is ignored in comparison.
        /// </summary>
        public int CompareTo(RuleVersion other)
        {
            var majorCompare = Major.CompareTo(other.Major);
            if (majorCompare != 0) return majorCompare;

            var minorCompare = Minor.CompareTo(other.Minor);
            if (minorCompare != 0) return minorCompare;

            var patchCompare = Patch.CompareTo(other.Patch);
            if (patchCompare != 0) return patchCompare;

            // Prerelease comparison: no prerelease > any prerelease
            if (Prerelease == null && other.Prerelease == null) return 0;
            if (Prerelease == null) return 1; // release > prerelease
            if (other.Prerelease == null) return -1;

            return string.CompareOrdinal(Prerelease, other.Prerelease);
        }

        /// <summary>
        /// Returns true if this version is compatible with the target version.
        /// Compatibility means: same major version, and this version is greater than or equal to target.
        /// </summary>
        /// <param name="target">The target/minimum version to check against.</param>
        /// <returns>True if this version is backward compatible with the target.</returns>
        public bool IsCompatibleWith(RuleVersion target)
        {
            // Same major version and this version is greater or equal to target
            return Major == target.Major && CompareTo(target) >= 0;
        }

        /// <summary>
        /// Returns a new version with incremented major version (minor and patch reset to 0).
        /// </summary>
        public RuleVersion IncrementMajor() => new(Major + 1, 0, 0);

        /// <summary>
        /// Returns a new version with incremented minor version (patch reset to 0).
        /// </summary>
        public RuleVersion IncrementMinor() => new(Major, Minor + 1, 0);

        /// <summary>
        /// Returns a new version with incremented patch version.
        /// </summary>
        public RuleVersion IncrementPatch() => new(Major, Minor, Patch + 1);

        /// <summary>
        /// Returns the version string in SemVer 2.0.0 format.
        /// </summary>
        public override string ToString()
        {
            var result = $"{Major}.{Minor}.{Patch}";
            if (!string.IsNullOrEmpty(Prerelease))
                result += $"-{Prerelease}";
            if (!string.IsNullOrEmpty(BuildMetadata))
                result += $"+{BuildMetadata}";
            return result;
        }

        public static bool operator >(RuleVersion a, RuleVersion b) => a.CompareTo(b) > 0;
        public static bool operator <(RuleVersion a, RuleVersion b) => a.CompareTo(b) < 0;
        public static bool operator >=(RuleVersion a, RuleVersion b) => a.CompareTo(b) >= 0;
        public static bool operator <=(RuleVersion a, RuleVersion b) => a.CompareTo(b) <= 0;
    }
}
