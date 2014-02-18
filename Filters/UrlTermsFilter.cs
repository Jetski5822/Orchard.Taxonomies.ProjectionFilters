using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Taxonomies.Models;
using Orchard.Taxonomies.Projections;
using Orchard.Taxonomies.Services;

namespace Orchard.Taxonomies.ProjectionFilters.Filters {
    public class UrlTermsFilter : IFilterProvider {
        private readonly ITaxonomyService _taxonomyService;
        private readonly IOrchardServices _orchardServices;

        public UrlTermsFilter(ITaxonomyService taxonomyService, IOrchardServices orchardServices) {
            _taxonomyService = taxonomyService;
            _orchardServices = orchardServices;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public void Describe(dynamic describe) {
            describe.For("Taxonomy", T("Taxonomy"), T("Taxonomy"))
                .Element("UrlTerms", T("Url Taxonomy"), T("Categorized content items"),
                    (Action<dynamic>)ApplyFilter,
                    (Func<dynamic, LocalizedString>)DisplayFilter,
                    "SelectTermsKey"
                );
        }

        public void ApplyFilter(dynamic context) {
            var httpContext = _orchardServices.WorkContext.HttpContext;
            var queryString = httpContext.Request.QueryString;
            var queryStringTerms = new List<string>();
            var termKey = ((string)context.State.TermKey) ?? "term";

            if (queryString[termKey] != null) {
                queryStringTerms = queryString.GetValues(termKey).SelectMany(x => x.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)).ToList();
            }

            if (queryStringTerms.Any()) {
                var ids = queryStringTerms.Select(Int32.Parse).ToArray();

                if (ids.Length == 0) {
                    return;
                }

                int op = Convert.ToInt32(context.State.Operator);

                var terms = ids.Select(_taxonomyService.GetTerm).ToList();
                var allChildren = new List<TermPart>();
                foreach (var term in terms) {
                    allChildren.AddRange(_taxonomyService.GetChildren(term));
                    allChildren.Add(term);
                }

                allChildren = allChildren.Distinct().ToList();

                var predicates = allChildren.Select(localTerm => (Action<IHqlExpressionFactory>)(a => a.Eq("Id", localTerm.Id))).ToList();

                switch (op) {
                    case 0:
                        // is one of
                        Action<IAliasFactory> selector1 = alias => alias.ContentPartRecord<TermsPartRecord>().Property("Terms", "terms").Property("TermRecord", "termRecord");
                        Action<IHqlExpressionFactory> filterA = x => x.Disjunction(predicates.Take(1).Single(), predicates.Skip(1).ToArray());
                        context.Query.Where(selector1, filterA);
                        break;
                    case 1:
                        // is not one of
                        Action<IAliasFactory> selector = alias => alias.ContentPartRecord<TermsPartRecord>().Property("Terms", "terms").Property("TermRecord", "termRecord");
                        Action<IHqlExpressionFactory> filterB = x => x.Disjunction(predicates.Take(1).Single(), predicates.Skip(1).ToArray());
                        context.Query.Where(selector, filterB);
                        break;
                    case 2:
                        // is all of
                        break;
                }
            }
        }

        public LocalizedString DisplayFilter(dynamic context) {
            string key = Convert.ToString(context.State.TermKey);

            if (string.IsNullOrWhiteSpace(key))
                key = "term";
            
            return T("Categorized with any terms specified via the querystring with key \"{0}\"", key);
        }
    }

}