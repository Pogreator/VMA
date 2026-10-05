import argparse
import subprocess
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(
        description="Processes example .vma files into .bin files."
    )
    parser.add_argument(
        "vma_path", type=str, help="Path to the vma executable"
    )
    args = parser.parse_args()

    source_dir = Path("examples/source")
    bin_dir = Path("examples/bin")

    # Verify that the source directory exists
    if not source_dir.is_dir():
        print(f"Error: Source directory '{source_dir}' does not exist.")
        return

    # Create the output directory if it doesn't exist yet
    bin_dir.mkdir(parents=True, exist_ok=True)

    # Find all .vma files in the source folder
    vma_files = list(source_dir.glob("*.vma"))

    if not vma_files:
        print(f"No .vma files found in '{source_dir}'.")
        return

    print(f"Found {len(vma_files)} file(s) to process.\n")

    # Process each file individually
    for input_file in vma_files:
        # Determine the corresponding output file name (.vma -> .bin)
        output_file = bin_dir / f"{input_file.stem}.bin"

        # Construct the command list
        command = [
            args.vma_path,
            "-i",
            str(input_file),
            "-o",
            str(output_file),
        ]

        print(f"Running: {' '.join(command)}")

        try:
            result = subprocess.run(
                command, check=True, capture_output=True, text=True
            )
            print(f"Successfully processed {input_file.name}")
        except subprocess.CalledProcessError as e:
            print(f"Error processing {input_file.name}:")
            print(e.stderr)
        print("-" * 40)


if __name__ == "__main__":
    main()