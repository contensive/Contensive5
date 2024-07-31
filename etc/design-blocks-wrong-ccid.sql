



select 'update '+t.name+' set contentcontrolid='+cast(c.id as nvarchar(10)) from cccontent c left join cctables t on t.id=c.ContentTableID where t.name like 'db%'

--update dbaccordion set contentcontrolid=229
--update dbCarousel set contentcontrolid=226
--update dbContactUs set contentcontrolid=230
--update dbContactUsData set contentcontrolid=231
--update DbcontactUsrules set contentcontrolid=232
--update dbdirectorymembershiptyperules set contentcontrolid=192
--update dbFonts set contentcontrolid=132
--update dbfourcolumn set contentcontrolid=233
--update dbGroupDirectories set contentcontrolid=252
--update dbHeadlineWidgets set contentcontrolid=266
--update dbHeroImage set contentcontrolid=234
--update DBHtmlEmbedCode set contentcontrolid=235
--update dbimagegallery set contentcontrolid=236
--update dbImageGalleryCards set contentcontrolid=237
--update dbImageGalleryimages set contentcontrolid=238
--update dbImages set contentcontrolid=247
--update dbImageSliders set contentcontrolid=239
--update dbLogin set contentcontrolid=240
--update dbMockWidgets set contentcontrolid=257
--update dbOneColumn set contentcontrolid=241
--update dbPageExtensions set contentcontrolid=242
--update dbSliders set contentcontrolid=243
--update dbTestimonials set contentcontrolid=256
--update dbTestimonialWidgets set contentcontrolid=254
--update dbText set contentcontrolid=244
--update dbTextAndImage set contentcontrolid=245
--update dbThemes set contentcontrolid=133
--update dbthreecolumn set contentcontrolid=246
--update dbTiles set contentcontrolid=248
--update dbTwoColumn set contentcontrolid=249
--update dbTwoColumnLeft set contentcontrolid=250
--update dbTwoColumnRight set contentcontrolid=251
--update dbYouTubeWidgets set contentcontrolid=227