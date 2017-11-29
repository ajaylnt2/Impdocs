function trgUpdateMetaData() {
    var context = getContext();
    var collection = context.getCollection();
    var collectionLink = collection.getSelfLink();
    var response = context.getResponse();

    var createDoc = response.getBody();

    var metadataDocument;

    collection.queryDocuments(collectionLink, 'select * from c where c.id = "_metadata"', {},
        function (err, results) {
            if (err) {
                throw new Error('Error querying for metadata document: ' + err.message);
            }
            if (results.length == 1) {
                metadataDocument = results[0];
                updateMetadataDocument();
            }
            else {
                collection.createDocument(collectionLink, { id: "_metadata" }, {},
                    function (err, createdMetadataDocument) {
                        if (err) {
                            throw new Error('Error creating for metadata document: ' + err.message);
                        }
                        metadataDocument = createdMetadataDocument;
                        updateMetadataDocument();
                    }
                );
            }
        }
    );

    function updateMetadataDocument() {
        // Get the meta document. We keep it in the same collection. it's the only doc that has .isMetadata = true.
        metadataDocument.lastId = createDoc.id;
        collection.replaceDocument(metadataDocument._self, metadataDocument,
            function (err) {
                if (err) {
                    throw new Error('Error updating for metadata document: ' + err.message);
                }
            });
    }
}