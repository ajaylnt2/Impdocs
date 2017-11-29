
function Insert(transactionId, docs) {

    var collection = getContext().getCollection();
    var collectionLink = collection.getSelfLink();

    // The count of imported docs, also used as current doc index.
    var count = 0;

    // Validate input.
    if (!docs) throw new Error("The array is undefined or null.");
    if (!transactionId) throw new Error("The transactionId is undefined or null.")


    var docsLength = docs.length;
    if (docsLength == 0) {
        getContext().getResponse().setBody(0);
        return;
    }
    newCreate(docs[count], callback);


    function newCreate(doc, callback) {
        var query = { query: "select * from c where c.id = @id", parameters: [{ name: "@id", value: "docs.id"}]};
        doc.TransactionId = transactionId;
        var isAccepted = collection.createDocument(collectionLink, query, callback);

        // If the request was accepted, callback will be called.
        // Otherwise report current count back to the client,
        if (!isAccepted) getContext().getResponse().setBody(count);
    }

    // This is called when collection.createDocument is done and the document has been persisted.
    function callback(err, doc, options) {
        if (err) throw err;

        // One more document has been inserted, increment the count.
        count++;

        if (count >= docsLength) {
            // If we have created all documents, we are done. Just set the response.
            getContext().getResponse().setBody(count);
        }
        else {
            // Create next document.
            newCreate(docs[count], callback);
        }
    }
}
