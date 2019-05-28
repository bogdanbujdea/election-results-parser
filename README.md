# election-results-parser
This is a small tool for processing election data and publishing it with an Azure Function.

#### Disclaimer: I don't work for BEC and I'm not associated in any way with them. The results are not official.

#### Disclaimer2: Don't judge the code, it was written in half an hour after I saw that the votes are being counted but no statistics were available.

# How it works
The website https://prezenta.bec.ro/europarlamentare26052019 offers CSV files with the counted votes in Romania and outside the country.

I just download them every 5 minutes with a timed Azure Function, and then I store them inside an Azure Blob.

The results are available from an HTTP triggered function, which downloads the CSV from the blob and parses it.

After that, I build a basic HTML and return it in the response.

You can check it here: http://bit.ly/rezultateprovizorii

If you wanna help build stuff like this, join [Code 4 Romania](https://tfsg.code4.ro/ro/), we are a group of volunteers who create open source digital tools that upgrade Romania to a better place to live in.


And if you don't have time, [donations are always welcomed](https://code4.ro/ro/doneaza/)
