using AssetModel;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AssetServices.Controllers
{
    [Route("api/[controller]")]
    public class AssetController : Controller
    {
        private AssetManager assetManager;

        public AssetController()
        {
            assetManager = new AssetManager();
        }

        // GET api/v1/[controller]/items
        // create the asset..
        [HttpPost("AddAsset")]
        public async Task<IActionResult> CreateAsset(Asset asset)
        {
            var data = await assetManager.CreateAssetDocumentIfNotExists(asset);
            return Ok(data);
        }
        //  Delete the asset based on the id..
        [HttpGet("DeleteAsset/{id}")]
        public async Task<IActionResult> DeleteAsset(string assetId)
        {
            await assetManager.DeleteAssetbyId(assetId);
            return Ok("Asset Deleted !");
        }
        // Edit the asset  
        [HttpPost("EditAsset")]
        public ActionResult EditAsset(Asset asset)
        {
            assetManager.EditAsset(asset);
            return Ok("Asset Edited !");
        }
        // fetch the asset based on asset Id
        [HttpGet("GetAssetById/{id}")]
        public async Task<IActionResult> GetAssetById(string AssetId)
        {
            var asset = await assetManager.GetAssetById(AssetId);
            return Ok(asset);
        }
        // Get the asset type based on the asset type
        [HttpGet("GetAssetByType/{Type}")]
        public IActionResult GetAssetByType(string assetType)
        {
            var data = assetManager.GetAssetByType(assetType);
            return Ok(data);
        }
        // Get the  asset Data source based on asset Id..
        [HttpGet("GetAssetDataSource/{id}")]
        public async Task<IActionResult> GetAssetDataSource(string assetId)
        {
            var data = await assetManager.GetAssetDataSource(assetId);
            return Ok(data);
        }
        // Fetch all the assets
        [HttpGet]
        [Route("GetAssets")]
        public IActionResult GetAssets()
        {
            var data = assetManager.GetAssets();
            return Ok(data);
        }
        // Fetch the asset tags based on the asset id..
        [HttpGet("GetAssetTags/{id}")]
        public async Task<IActionResult> GetAssetTags(string assetId)
        {
            var data = await assetManager.GetAssetTags(assetId);
            return Ok();
        }
        // Fetch the tag based on the Id...
        [HttpGet("GetTag/{id}")]
        public IActionResult GetTag(int tagId)
        {
            var data = assetManager.GetTag(tagId);
            return Ok(data);
        }
        // fetch the  Tagdetails based on the tagId..

        [HttpGet("GetTagDetails/{id}")]
        public IActionResult GetTagDetails(int tagId)
        {
            var data = assetManager.GetTagDetails(tagId);
            return Ok(data);
        }
    }
}