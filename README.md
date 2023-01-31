The ScrollRectCarousel is a UGUI scrollRect companion that displays the data in an efficient manner.
Instead of instantiating a gameObject for every entry in your data, the carousel only instantiate the number needed to visually cover your scrollRect display area.  Then it carousels through the data items for display.

The CarouselScrollRect uses the UGUI ScrollRect along with a ScrollContent gameObject and ItemContainer GameObject to facilitate the drag & scroll then the CarouselItemList container to display the carousel items.


The CarouselScrollRect uses a MVC architecture and the package includes the following core files

ScrollRectCarouselDataAbstract - An abstract base class for line item model to inherit from.  This provides a way for the Controller & View to work with the model

ScrollRectCarouselItemList - This is the core of the carousel scroll rect.  It contains logic for instantiating the needed list items and it controls how items are updated and displayed in the list.  It has a corresponding example prefab CarouselScrollRect and is used in conjunction with the UGUI scrollRect and must use various ScrollRectCarousel interfaces and the base dataAbstract class to function.

IScrollRectCarouselDataController - This interface provides the user a way to request a view refresh by having access to the list of data and reference to the view interface for render.  

IScrollRectLineItemView - This is the interface for the individual line item display.  Implementing this will provide the core scroll list with necessary info like line item width/height, min width/height which is will use to populate the scroll list.  This also gives the line item a way to be update given the data.

The CarouselScrollRect package also includes the CarouselScrollRect prefab, this is a springboard prefab that has the setup for a functioning carousel scrollrect that can be customized.