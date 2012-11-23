#pragma once

using namespace System;
using namespace System::Drawing;
using namespace System::Diagnostics;
using namespace System::IO;
using namespace System::Text;
using namespace System::Collections::Generic;
using namespace Research::GraphBasedShapePrior;
using namespace Research::GraphBasedShapePrior::Util;

public ref class LearningTracker {
private:
	static int outerIteration;
	static int innerIteration;
	static String ^loggingDir;

	static void DrawShape(Graphics ^graphics, Color color, Shape ^shape)
    {
        const float pointRadius = 2;
        const float lineWidth = 1;

        for (int i = 0; i < shape->VertexPositions->Count; ++i)
        {
            graphics->FillEllipse(
                Brushes::Black,
                static_cast<float>(shape->VertexPositions[i].X - pointRadius),
                static_cast<float>(shape->VertexPositions[i].Y - pointRadius),
                2 * pointRadius,
                2 * pointRadius);
        }

        for (int i = 0; i < shape->Structure->Edges->Count; ++i)
        {
            ShapeEdge edge = shape->Structure->Edges[i];
            Vector point1 = shape->VertexPositions[edge.Index1];
            Vector point2 = shape->VertexPositions[edge.Index2];
            graphics->DrawLine(gcnew Pen(Color::Black, lineWidth), MathHelper::VecToPointF(point1), MathHelper::VecToPointF(point2));
			DrawOrientedRectange(graphics, gcnew Pen(color, lineWidth), point1, point2, static_cast<float>(shape->EdgeWidths[i]));
        }
    }

    static void DrawOrientedRectange(Graphics ^graphics, Pen ^pen, Vector point1, Vector point2, float sideWidth)
    {
		Vector diff = point1 - point2;
		Vector sideDirection = Vector(diff.Y, -diff.X).GetNormalized();
			
		array<PointF> ^points = gcnew array<PointF>(4);
		points[0] = MathHelper::VecToPointF(point1 - sideDirection * sideWidth * 0.5);
		points[1] = MathHelper::VecToPointF(point1 + sideDirection * sideWidth * 0.5);
		points[2] = MathHelper::VecToPointF(point2 + sideDirection * sideWidth * 0.5);
		points[3] = MathHelper::VecToPointF(point2 - sideDirection * sideWidth * 0.5);
			
		graphics->DrawPolygon(pen, points);
    }

	static void ReportDoubleValue(String ^fileName, double value) {
		FileStream ^stream = gcnew FileStream(Path::Combine(loggingDir, fileName), FileMode::Append);
		StreamWriter ^writer = gcnew StreamWriter(stream);
		writer->WriteLine("{0:000}\t{1:0000}\t{2:0.0000}", outerIteration, innerIteration, value);
		writer->Close();
	}

	static void ReportDoubleValueArray(String ^fileName, array<double> ^values) {
		FileStream ^stream = gcnew FileStream(Path::Combine(loggingDir, fileName), FileMode::Append);
		StreamWriter ^writer = gcnew StreamWriter(stream);
		writer->Write("{0:000}\t{1:0000}", outerIteration, innerIteration);
		for (int i = 0; i < values->Length; ++i)
			writer->Write("\t{0:0.0000}", values[i]);
		writer->WriteLine();
		writer->Close();
	}

	static void ReportPerSampleDoubleValueArray(String ^fileName, int sample, array<double> ^values) {
		FileStream ^stream = gcnew FileStream(Path::Combine(loggingDir, fileName), FileMode::Append);
		StreamWriter ^writer = gcnew StreamWriter(stream);
		writer->Write("{0:000}\t{1:0000}\t{2}", outerIteration, innerIteration, sample);
		for (int i = 0; i < values->Length; ++i)
			writer->Write("\t{0:0.0000}", values[i]);
		writer->WriteLine();
		writer->Close();
	}

public:
	static LearningTracker()
	{
		outerIteration = 0;
		innerIteration = 0;

		loggingDir = gcnew String("Log");
		if (Directory::Exists(loggingDir))
			Directory::Delete(loggingDir, true);
		Directory::CreateDirectory(loggingDir);
	}

	static void NextOuterIteration() {
		++outerIteration;
		innerIteration = 0;
	}

	static void NextInnerIteration() {
		++innerIteration;
	}

	static void ReportWeights(double *weights, size_t weightCount) {
		array<double> ^values = gcnew array<double>(weightCount);
		for (size_t i = 0; i < weightCount; ++i)
			values[i] = weights[i + 1];
		ReportDoubleValueArray("weights.txt", values);
	}

	static void ReportLoss(int sample, double vertexLoss, double edgeLoss) {
		array<double> ^values = gcnew array<double>(2);
		values[0] = vertexLoss;
		values[1] = edgeLoss;
		ReportPerSampleDoubleValueArray("loss.txt", sample, values);
	}

	static void ReportLowerBound(double value) {
		ReportDoubleValue("lower_bound.txt", value);
	}

	static void ReportUpperBound(double value) {
		ReportDoubleValue("upper_bound.txt", value);
	}

	static void ReportInferredLatentVariables(int sampleIndex, Shape ^desiredShape, Image2D<bool> ^mask) {
		Bitmap ^canvas = gcnew Bitmap(mask->Width, mask->Height);
        Graphics ^graphics = Graphics::FromImage(canvas);
		graphics->DrawImage(Image2D::ToRegularImage(mask), 0, 0, canvas->Width, canvas->Height);

		String ^fileName = String::Format("latent_{0:000}_{1:000}.png", outerIteration, sampleIndex);
		Image2D::SaveToFile(Image2D::FromRegularImage(canvas), Path::Combine(loggingDir, fileName));
	}

	static void ReportGroundTruth(
		int sampleIndex,
		Image2D<Color> ^image,
		Shape ^trueShape,
		Image2D<ObjectBackgroundTerm> ^colorTerms,
		Image2D<ObjectBackgroundTerm> ^shapeTerms,
		Image2D<double> ^horizontalPairwiseTerms)
	{
		Bitmap ^canvas = gcnew Bitmap(image->Width, image->Height);
		Graphics ^graphics = Graphics::FromImage(canvas);
		graphics->DrawImage(Image2D::ToRegularImage(image), 0, 0, image->Width, image->Height);
		DrawShape(graphics, Color::Blue, trueShape);
		
		Image2D::SaveToFile(Image2D::FromRegularImage(canvas), Path::Combine(loggingDir, String::Format("ground_truth_{0:000}.png", sampleIndex)));
		Image2D::SaveToFile(colorTerms, -4, 4, Path::Combine(loggingDir, String::Format("ground_truth_color_{0:000}.png", sampleIndex)));
		Image2D::SaveToFile(shapeTerms, -4, 4, Path::Combine(loggingDir, String::Format("ground_truth_shape_{0:000}.png", sampleIndex)));
		Image2D::SaveToFile(horizontalPairwiseTerms, Path::Combine(loggingDir, String::Format("ground_truth_pairwise_h_{0:000}.png", sampleIndex)));
	}
	
	static void ReportMostViolatedConstraint(int sampleIndex, Image2D<Color> ^image, Shape ^desiredShape, Shape ^foundShape, Image2D<bool> ^foundMask)
	{
		// Draw desired shape vs found shape
		{
			Bitmap ^imageCanvas = gcnew Bitmap(image->Width, image->Height);
			Graphics ^imageGraphics = Graphics::FromImage(imageCanvas);
			imageGraphics->DrawImage(Image2D::ToRegularImage(image), 0, 0, imageCanvas->Width, imageCanvas->Height);
			DrawShape(imageGraphics, Color::Red, foundShape);

			String ^imageFileName = String::Format("constraint_{0:000}_{1:0000}_{2:000}.png", outerIteration, innerIteration, sampleIndex);
			Image2D::SaveToFile(Image2D::FromRegularImage(imageCanvas), Path::Combine(loggingDir, imageFileName));
		}

		// Draw found mask & shape
		{
			Bitmap ^maskCanvas = gcnew Bitmap(foundMask->Width, foundMask->Height);
			Graphics ^maskGraphics = Graphics::FromImage(maskCanvas);
			maskGraphics->DrawImage(Image2D::ToRegularImage(foundMask), 0, 0, maskCanvas->Width, maskCanvas->Height);
			DrawShape(maskGraphics, Color::Red, foundShape);

			String ^maskFileName = String::Format("constraint_mask_{0:000}_{1:0000}_{2:000}.png", outerIteration, innerIteration, sampleIndex);
			Image2D::SaveToFile(Image2D::FromRegularImage(maskCanvas), Path::Combine(loggingDir, maskFileName));
		}
	}
};