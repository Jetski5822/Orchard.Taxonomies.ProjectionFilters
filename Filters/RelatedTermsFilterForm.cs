using System;
using System.Web.Mvc;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Taxonomies.Projections;
using Orchard.Taxonomies.Services;

namespace Orchard.Taxonomies.ProjectionFilters.Filters {
    
    public class RelatedTermsFilterForm : IFormProvider {
        private readonly ITaxonomyService _taxonomyService;
        protected dynamic Shape { get; set; }
        public Localizer T { get; set; }

        public RelatedTermsFilterForm(
            IShapeFactory shapeFactory, ITaxonomyService taxonomyService) {
            Shape = shapeFactory;
            _taxonomyService = taxonomyService;
            T = NullLocalizer.Instance;
        }

        public void Describe(dynamic context) {
            Func<IShapeFactory, object> form =
                shape => {

                    var f = Shape.Form(
                        Id: "TaxonomyId",
                        _IncludeSelf: Shape.Checkbox(
                            Id: "includeSelf", Name: "IncludeSelf",
                            Title: T("Include currently viewed content item."), Value: "0"
                            ),
                        _TaxonomyId: Shape.SelectList(
                            Id: "TaxonomyId", Name: "TaxonomyId",
                            Title: T("Taxonomy"),
                            Description: T("Select the taxonomy to select the related items from.")
                            )
                        );

                    f._TaxonomyId.Add(new SelectListItem { Value = "", Text = "" });
                    foreach (var taxonomy in _taxonomyService.GetTaxonomies()) {
                        f._TaxonomyId.Add(new SelectListItem {Value = taxonomy.Id.ToString(), Text = taxonomy.Name});
                    }

                    return f;
                };

            context.Form("RelatedTaxonomy", form);

        }
    }
}