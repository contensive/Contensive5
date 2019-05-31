﻿

using System;
using System.Collections.Generic;
using Contensive.BaseClasses;
using Contensive.Processor;
using Contensive.Processor.Controllers;
using Contensive.Processor.Models.Domain;
using static Newtonsoft.Json.JsonConvert;
//
namespace Contensive.Addons.AddonListEditor {
    public class SetSampleAddonListClass : AddonBaseClass {
        // 
        // ====================================================================================================
        // 
        public override object Execute(CPBaseClass cp) {
            try {
                CoreController core = ((CPClass)cp).core;
                // 
                SetAddonList_RequestClass request = DeserializeObject<SetAddonList_RequestClass>(cp.Request.Form);
                if (request == null) {
                    //
                    // -- allow simple querystring values
                    request = new SetAddonList_RequestClass() {
                        parentRecordGuid = cp.Doc.GetText("parentRecordGuid"),
                        parentContentGuid = cp.Doc.GetText("parentContentGuid"),
                        addonList = new List<AddonListItemModel>()
                    };
                }
                //
                LogController.logTrace(core, "request.parentRecordGuid [" + request.parentRecordGuid + "], request.parentContentGuid [" + request.parentContentGuid + "]");
                //
                if (String.IsNullOrWhiteSpace(request.parentContentGuid)) {
                    return SerializeObject(new SetAddonList_ResponseClass() {
                        errorList = new List<string> { "The request parent content guid is not valid [" + request.parentContentGuid + "]" }
                    });
                }
                if (String.IsNullOrWhiteSpace(request.parentRecordGuid)) {
                    return SerializeObject(new SetAddonList_ResponseClass() {
                        errorList = new List<string> { "The request parent record guid is not valid [" + request.parentRecordGuid + "]" }
                    });
                }
                var metadata = Contensive.Processor.Models.Domain.ContentMetadataModel.create(core, request.parentContentGuid);
                if (metadata == null) {
                    return SerializeObject(new SetAddonList_ResponseClass() {
                        errorList = new List<string> { "The parent content could not be determined from the guid [" + request.parentContentGuid + "]" }
                    });
                }
                //
                // -- create sample addonList
                request.addonList = new List<AddonListItemModel>() {
                    new AddonListItemModel() {
                        isStructural = false,
                        designBlockTypeGuid = guidDesignBlockHeroImage,
                        instanceGuid = GenericController.createGuid(),
                        addonList = null
                    },
                    new AddonListItemModel() {
                        isStructural = false,
                        designBlockTypeGuid = guidDesignBlockContactUs,
                        instanceGuid = GenericController.createGuid(),
                        addonList = null
                    },
                    new AddonListItemModel() {
                        isStructural = true,
                        designBlockTypeGuid = guidDesignBlockFourColumn,
                        instanceGuid = GenericController.createGuid(),
                        addonList = new List<List<AddonListItemModel>>() {
                            new List<AddonListItemModel>() {
                                new AddonListItemModel() {
                                    isStructural = false,
                                    designBlockTypeGuid = guidDesignBlockTile,
                                    instanceGuid = GenericController.createGuid(),
                                    addonList = null
                                }
                            },
                            new List<AddonListItemModel>() {
                                new AddonListItemModel() {
                                    isStructural = false,
                                    designBlockTypeGuid = guidDesignBlockTile,
                                    instanceGuid = GenericController.createGuid(),
                                    addonList = null
                                }
                            },
                            new List<AddonListItemModel>() {
                                new AddonListItemModel() {
                                    isStructural = false,
                                    designBlockTypeGuid = guidDesignBlockTile,
                                    instanceGuid = GenericController.createGuid(),
                                    addonList = null
                                }
                            },
                            new List<AddonListItemModel>() {
                                new AddonListItemModel() {
                                    isStructural = false,
                                    designBlockTypeGuid = guidDesignBlockTile,
                                    instanceGuid = GenericController.createGuid(),
                                    addonList = null
                                }
                            }
                        }
                    },
                    new AddonListItemModel() {
                        isStructural = true,
                        designBlockTypeGuid = guidDesignBlockTwoColumn,
                        instanceGuid = GenericController.createGuid(),
                        addonList = new List<List<AddonListItemModel>>() {
                            new List<AddonListItemModel>() {
                                new AddonListItemModel() {
                                    isStructural = false,
                                    designBlockTypeGuid = guidDesignBlockTile,
                                    instanceGuid = GenericController.createGuid(),
                                    addonList = null
                                },
                                new AddonListItemModel() {
                                    isStructural = false,
                                    designBlockTypeGuid = guidDesignBlockTile,
                                    instanceGuid = GenericController.createGuid(),
                                    addonList = null
                                }
                            },
                            new List<AddonListItemModel>() {
                                new AddonListItemModel() {
                                    isStructural = true,
                                    designBlockTypeGuid = guidDesignBlockTwoColumn,
                                    instanceGuid = GenericController.createGuid(),
                                    addonList = new List<List<AddonListItemModel>>() {
                                        new List<AddonListItemModel>() {
                                            new AddonListItemModel() {
                                                isStructural = false,
                                                designBlockTypeGuid = guidDesignBlockTile,
                                                instanceGuid = GenericController.createGuid(),
                                                addonList = null
                                            },
                                            new AddonListItemModel() {
                                                isStructural = false,
                                                designBlockTypeGuid = guidDesignBlockTile,
                                                instanceGuid = GenericController.createGuid(),
                                                addonList = null
                                            }
                                        },
                                        new List<AddonListItemModel>() {
                                            new AddonListItemModel() {
                                                isStructural = true,
                                                designBlockTypeGuid = guidDesignBlockTwoColumn,
                                                instanceGuid = GenericController.createGuid(),
                                                addonList = null
                                            },
                                            new AddonListItemModel() {
                                                isStructural = false,
                                                designBlockTypeGuid = guidDesignBlockText,
                                                instanceGuid = GenericController.createGuid(),
                                                addonList = null
                                            }
                                        },
                                    }
                                },
                                new AddonListItemModel() {
                                    isStructural = false,
                                    designBlockTypeGuid = guidDesignBlockText,
                                    instanceGuid = GenericController.createGuid(),
                                    addonList = null
                                }
                            },
                        }
                    },
                };
                //
                // -- validate addonList from UI and set back into a string
                switch (metadata.name.ToLower()) {
                    case "page content":
                        var page = Contensive.Processor.Models.Db.PageContentModel.create(core, request.parentRecordGuid);
                        if (page == null) {
                            return SerializeObject(new SetAddonList_ResponseClass() {
                                errorList = new List<string> { "The parent record in [Page Content] could not be determined from the guid [" + request.parentRecordGuid + "]" }
                            });
                        }
                        page.addonList = SerializeObject(request.addonList);
                        page.save(core);
                        break;
                    case "page templates":
                        var template = Contensive.Processor.Models.Db.PageTemplateModel.create(core, request.parentRecordGuid);
                        if (template == null) {
                            return SerializeObject(new SetAddonList_ResponseClass() {
                                errorList = new List<string> { "The parent record in [Page Templates] could not be determined from the guid [" + request.parentRecordGuid + "]" }
                            });
                        }
                        template.addonList = SerializeObject(request.addonList);
                        template.save(core);
                        break;
                    default:
                        return SerializeObject(new SetAddonList_ResponseClass() {
                            errorList = new List<string> { "The parent content is not valid addonList container [" + metadata.name + "]" }
                        });
                }
                //
                // -- now call getAddonList to return result
                cp.Doc.SetProperty("parentRecordGuid", request.parentRecordGuid);
                cp.Doc.SetProperty("parentContentGuid", request.parentContentGuid);
                return (new GetAddonListClass()).Execute(cp);
            }
            // 
            catch (Exception ex) {
                cp.Site.ErrorReport(ex);
                return string.Empty;
            }
        }
        // 
        public class SetAddonList_RequestClass {
            /// <summary>
            /// The guid of the content where this list is stored (content + record define location)
            /// </summary>
            public string parentContentGuid;
            /// <summary>
            /// The guid of the record where this list is stored (content + record define location)
            /// </summary>
            public string parentRecordGuid;
            /// <summary>
            /// A list of positions. Positions are the slots where a design block goes
            /// </summary>
            public List<AddonListItemModel> addonList;
        }
        // 
        public class SetAddonList_ResponseClass {
            /// <summary>
            /// The guid of the content where this list is stored (content + record define location)
            /// </summary>
            public List<string> errorList;
        }
        //
        // -- constants used only here - these are guids for addons used in the sample
        public const string guidDesignBlockTile = "{63EC50BE-4052-4594-B2AB-5F9739564DFA}";
        public const string guidDesignBlockLogin = "{3b18d3b1-916e-4bbb-b29d-b558beba5e6d}";
        public const string guidDesignBlockFourColumn = "{A9915665-3287-4C14-8FB5-039CC4C115DE}";
        public const string guidDesignBlockTwoColumn = "{48b9c19d-4b78-441e-8fab-d2b00c8a0e60}";
        public const string guidDesignBlockText = "{4F7FADCB-7B0B-4E4B-BBE4-CFAF4E49D548}";
        public const string guidDesignBlockTextAndImage = "{C9473F1B-4210-43DC-B436-ACC08DA5594F}";
        public const string guidDesignBlockThreeColumn = "{2270DFF7-9043-43B4-87CE-CF1DDDBE99A1}";
        public const string guidDesignBlockTwoColumnLeft = "{A7D36B3A-C2C9-4985-8BA9-0A0DB65E7879}";
        public const string guidDesignBlockTwoColumnRight = "{81D90418-AC66-45BD-B756-64D748D1C504}";
        public const string guidDesignBlockCarousel = "{0c5e159e-71ea-45eb-9bfd-bdbba53e9f2a}";
        public const string guidDesignBlockContactUs = "{E136B8C6-629C-43E2-ABCB-295BA339577F}";
        public const string guidDesignBlockHeroImage = "{B3072A9B-8833-4DF1-982F-3061A060585E}";
        public const string guidDesignBlockImageSlider = "{d0b5aedd-f46a-4080-9ba6-ba5e8b2739f0}";
        public const string guidDesignBlockSlider = "{386A54CD-5A32-45EC-827F-D51E20D0DD58}";
        public const string guidDesignBlockAccordian = "{D7162FEC-9ED7-40F9-8285-2A506E5F8351}";
    }
}

