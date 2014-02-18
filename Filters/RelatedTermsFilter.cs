using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Alias;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Taxonomies.Models;
using Orchard.Taxonomies.Projections;
using Orchard.Taxonomies.Services;

namespace Orchard.Taxonomies.ProjectionFilters.Filters {
    public class RelatedFilter : IFilterProvider {
        private readonly ITaxonomyService _taxonomyService;
        private readonly IOrchardServices _orchardServices;
        private readonly IAliasService _aliasService;

        public RelatedFilter(ITaxonomyService taxonomyService, IOrchardServices orchardServices, IAliasService aliasService) {
            _taxonomyService = taxonomyService;
            _orchardServices = orchardServices;
            _aliasService = aliasService;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public void Describe(dynamic describe) {
            describe.For("Taxonomy", T("Taxonomy"), T("Taxonomy"))
                .Element("RelatedTerms", T("Related Taxonomy"), T("Categorized content items"),
                    (Action<dynamic>)ApplyFilter,
                    (Func<dynamic, LocalizedString>)DisplayFilter,
                    "RelatedTaxonomy"
                );
        }

        public void ApplyFilter(dynamic context) {
            var httpContext = _orchardServices.WorkContext.HttpContext;
            var path = httpContext.Request.AppRelativeCurrentExecutionFilePath.Replace("~/", string.Empty);

            var routeValues = _aliasService.Get(path);

            if (routeValues == null)
                return;

            object id;
            if (routeValues.TryGetValue("id", out id)) {
                var castedId = Convert.ToInt32(id);
                var ids = _taxonomyService.GetTermsForContentItem(castedId).Select(o => o.Id).ToList();

                if (ids.Any()) {
                    int op = Convert.ToInt32(context.State.Operator);
                    int? taxonomyId = context.State.TaxonomyId != null ? Convert.ToInt32(context.State.TaxonomyId) : default(int?);
                    var terms = ids.Select(_taxonomyService.GetTerm);
                    var allChildren = new List<TermPart>();

                    if (taxonomyId != null) {
                        var selectedTerm = _taxonomyService.GetTermsForContentItem(castedId).FirstOrDefault(x => x.TaxonomyId == taxonomyId);

                        if (selectedTerm != null)
                            terms = terms.Where(x => x.Id == selectedTerm.Id);
                    }

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

                            if (context.State.IncludeSelf == null) {
                                Action<IAliasFactory> selector2 = alias => alias.ContentItem();
                                Action<IHqlExpressionFactory> filter2 = x => x.Eq("Id", castedId);
                                Action<IHqlExpressionFactory> filter2Not = x => x.Not(filter2);
                                context.Query.Where(selector2, filter2Not);
                            }

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
        }

        public LocalizedString DisplayFilter(dynamic context) {
            return T("All content items that share terms of the currently displayed content item");
        }
    }

}