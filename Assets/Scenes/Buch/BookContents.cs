using TMPro;
using UnityEngine;


namespace ChristinaCreatesGames.Typography.Book
{
    public class BookContents : MonoBehaviour
    {
        [TextArea(10, 20)]
		[SerializeField] private string content;
        [Space]
		[SerializeField] private TMP_Text leftSide;
        [SerializeField] private TMP_Text rightSide;
        [Space]
		[SerializeField] private TMP_Text leftPagination;
        [SerializeField] private TMP_Text rightPagination;

        private void OnValidate()
        {
            UpdatePagination();

            if (leftSide.text == content)
                return;

            SetupContent();
        }

        private void Awake()
        {
            SetupContent();
            UpdatePagination();
        }
	
		//Initial text-setting
        private void SetupContent()
        {
            leftSide.text = content;
            rightSide.text = content;
        }

		//Updates the numbers displayed at the bottom of the book.
        private void UpdatePagination()
        {
            leftPagination.text = leftSide.pageToDisplay.ToString();
            rightPagination.text = rightSide.pageToDisplay.ToString();
        }
		
		
        public void PreviousPage()
        {
			//Clamp previous page to 1, as books have no Page 0.
            if (leftSide.pageToDisplay < 1)
            {
                leftSide.pageToDisplay = 1;
                return;
            }
			
            if (leftSide.pageToDisplay - 2 > 1)
                leftSide.pageToDisplay -= 2;
            else
                leftSide.pageToDisplay = 1;

            rightSide.pageToDisplay = leftSide.pageToDisplay + 1;

            UpdatePagination();
        }


        public void NextPage()
        {
            if (rightSide.pageToDisplay >= rightSide.textInfo.pageCount)
                return;

            if (leftSide.pageToDisplay >= leftSide.textInfo.pageCount - 1)
            {
                leftSide.pageToDisplay = leftSide.textInfo.pageCount - 1;
                rightSide.pageToDisplay = leftSide.pageToDisplay + 1;
            }
            else
            {
                leftSide.pageToDisplay += 2;
                rightSide.pageToDisplay = leftSide.pageToDisplay + 1;
            }

            UpdatePagination();
        }
    }
}