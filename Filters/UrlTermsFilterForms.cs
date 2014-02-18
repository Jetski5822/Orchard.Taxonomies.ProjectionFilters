using System;
using Orchard.DisplayManagement;
using Orchard.Localization;
using Orchard.Taxonomies.Projections;

namespace Orchard.Taxonomies.ProjectionFilters.Filters {
    
    public class UrlTermsFilterForms : IFormProvider {
        protected dynamic Shape { get; set; }
        public Localizer T { get; set; }

        public UrlTermsFilterForms(
            IShapeFactory shapeFactory) {
            Shape = shapeFactory;
            T = NullLocalizer.Instance;
        }

        public void Describe(dynamic context) {
            Func<IShapeFactory, object> form =
                shape => {

                    var f = Shape.Form(
                        Id: "SelectTermsKey",
                        _TermKey: Shape.TextBox(
                            Id: "termkey", Name: "TermKey",
                            Title: T("QueryString Key"),
                            Description: T("Specify the QueryString key to get the terms from.")
                            ),
                        _Exclusion: Shape.FieldSet(
                            _OperatorOneOf: Shape.Radio(
                                Id: "operator-is-one-of", Name: "Operator",
                                Title: T("Is one of"), Value: "0", Checked: true
                                ),
                            _OperatorIsAllOf: Shape.Radio(
                                Id: "operator-is-all-of", Name: "Operator",
                                Title: T("Is all of"), Value: "1"
                                )
                            )
                        );

                    return f;
                };

            context.Form("SelectTermsKey", form);

        }
    }
}