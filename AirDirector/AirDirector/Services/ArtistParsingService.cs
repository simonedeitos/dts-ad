using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AirDirector.Services.Database;

namespace AirDirector.Services
{
    public static class ArtistParsingService
    {
        // Separatori nel campo Artist (ordinati dal più specifico al meno specifico)
        private static readonly string[] ArtistSeparators = new[]
        {
            " feat. ", " feat ", " ft. ", " ft ",
            " vs. ", " vs ", " & ", " % ", " e ", ", "
        };

        // Pattern nel titolo: (feat. XXX), (ft. XXX), feat. XXX, ft. XXX
        private static readonly Regex TitleFeatRegex = new Regex(
            @"[\(\[]?\s*(?:feat\.?|ft\.?)\s+(.+?)[\)\]]?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Estrae artisti secondari dal campo Artist e dal Titolo.
        /// Restituisce (artistaPrincipale, listaArtistiSecondari)
        /// </summary>
        public static (string PrimaryArtist, List<string> FeaturedArtists) ParseArtists(
            string artistField, string titleField, List<ArtistAliasEntry> aliases = null)
        {
            var featured = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string primaryArtist = artistField?.Trim() ?? "";
            bool artistSplitDetected = false;

            // 1. Parse del campo Artist per separatori
            foreach (var sep in ArtistSeparators)
            {
                int idx = primaryArtist.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    artistSplitDetected = true;
                    string rest = primaryArtist.Substring(idx + sep.Length);
                    primaryArtist = primaryArtist.Substring(0, idx).Trim();

                    // La parte rimanente può contenere ulteriori artisti separati da virgola/&
                    var subSeps = new[] { ", ", " & ", " e ", " % " };
                    var subParts = rest.Split(subSeps, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in subParts)
                    {
                        string trimmed = part.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                            featured.Add(trimmed);
                    }
                    break;
                }
            }

            if (artistSplitDetected && !string.IsNullOrWhiteSpace(primaryArtist))
                featured.Add(primaryArtist);

            // 2. Parse del titolo per feat/ft tra parentesi o no
            if (!string.IsNullOrEmpty(titleField))
            {
                var match = TitleFeatRegex.Match(titleField);
                if (match.Success)
                {
                    string featPart = match.Groups[1].Value.Trim();
                    // Potrebbe contenere più artisti separati da virgola/&/e
                    var subSeps = new[] { ", ", " & ", " e ", " % " };
                    var subParts = featPart.Split(subSeps, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in subParts)
                    {
                        string trimmed = part.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                            featured.Add(trimmed);
                    }
                }
            }

            // 3. Risolvi alias → nomi canonici
            if (aliases != null && aliases.Count > 0)
            {
                var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var name in featured)
                    resolved.Add(ResolveAlias(name, aliases));
                featured = resolved;

                primaryArtist = ResolveAlias(primaryArtist, aliases);
            }

            if (!artistSplitDetected)
                featured.Remove(primaryArtist);

            return (primaryArtist, featured.ToList());
        }

        /// <summary>
        /// Restituisce TUTTI gli artisti associati a un brano (principale + featured),
        /// risolvendo eventuali alias.
        /// </summary>
        public static HashSet<string> GetAllArtists(
            string artist, string featuredArtists, List<ArtistAliasEntry> aliases = null)
        {
            var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(artist))
            {
                var parsedArtists = ParseArtists(artist, string.Empty, aliases);

                if (!string.IsNullOrWhiteSpace(parsedArtists.PrimaryArtist))
                    all.Add(parsedArtists.PrimaryArtist.Trim());

                foreach (var featured in parsedArtists.FeaturedArtists)
                {
                    if (!string.IsNullOrWhiteSpace(featured))
                        all.Add(featured.Trim());
                }
            }

            if (!string.IsNullOrWhiteSpace(featuredArtists))
            {
                foreach (var fa in featuredArtists.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string name = fa.Trim();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        string resolved = aliases != null ? ResolveAlias(name, aliases) : name;
                        all.Add(resolved);
                    }
                }
            }

            return all;
        }

        /// <summary>
        /// Controlla se due set di artisti si sovrappongono (tenendo conto degli alias).
        /// </summary>
        public static bool ArtistsOverlap(
            string artistA, string featuredA,
            string artistB, string featuredB,
            List<ArtistAliasEntry> aliases = null)
        {
            var setA = GetAllArtists(artistA, featuredA, aliases);
            var setB = GetAllArtists(artistB, featuredB, aliases);
            return setA.Overlaps(setB);
        }

        /// <summary>
        /// Risolve un nome artista al suo nome canonico tramite la lista di alias.
        /// Se non trovato, restituisce il nome originale.
        /// </summary>
        public static string ResolveAlias(string name, List<ArtistAliasEntry> aliases)
        {
            if (string.IsNullOrWhiteSpace(name) || aliases == null)
                return name;

            foreach (var entry in aliases)
            {
                // Controlla se è già il nome canonico
                if (string.Equals(entry.ArtistName?.Trim(), name, StringComparison.OrdinalIgnoreCase))
                    return entry.ArtistName.Trim();

                // Controlla nella lista alias
                if (!string.IsNullOrWhiteSpace(entry.Aliases))
                {
                    var aliasList = entry.Aliases.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (aliasList.Any(a => string.Equals(a.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                        return entry.ArtistName?.Trim() ?? name;
                }
            }

            return name;
        }
    }
}
